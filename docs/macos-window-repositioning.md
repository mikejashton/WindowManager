# macOS Window Repositioning — Architecture & Implementation Guide

## How Window Positioning Works on macOS via the Accessibility API

### The Core Mechanism: `AXUIElement`

macOS does not expose window handles as plain integers (unlike Windows `HWND`). Instead, every manageable window is accessed through **Accessibility API objects** (`AXUIElement`). These objects are opaque references to a running app's UI tree.

All window geometry is read and written through **accessibility attributes**:

| Attribute    | Type                         | Purpose                        |
| ------------ | ---------------------------- | ------------------------------ |
| `AXPosition` | `AXValue` wrapping `CGPoint` | Top-left origin of the window  |
| `AXSize`     | `AXValue` wrapping `CGSize`  | Width and height of the window |

These are not raw structs — they must be encoded/decoded through `AXValue`, which is an opaque wrapper that tells the API what concrete type the bytes inside represent (`CGPoint`, `CGSize`, or `CGRect`).

**Reading position/size:**

1. Call `AXUIElementCopyAttributeValue(element, kAXPositionAttribute, &value)`.
2. Cast the resulting `AXValueRef` to your concrete type via `AXValueGetValue(value, kAXValueCGPointType, &point)`.

**Writing position/size:**

1. Fill a `CGPoint` struct with the target `x`/`y`.
2. Pack it into an `AXValue` via `AXValueCreate(kAXValueCGPointType, &point)`.
3. Call `AXUIElementSetAttributeValue(element, kAXPositionAttribute, ax_value)`.
4. Repeat for `kAXSizeAttribute` with a `CGSize`.

**Critical quirk: set size before position, then size again.** Many apps (notably Firefox and Electron apps) apply the first size set relative to the old position, then shift position, causing a visual glitch. GlazeWM works around this with the sequence: `AXSize → AXPosition → AXSize`. The second size call corrects any drift.

**Another quirk: `AXEnhancedUserInterface`.** Some apps enable this attribute on their own `AXUIElement` (the app-level element, not the window-level element). When it is enabled, window layout is driven by the app's internal layout engine, overriding your `AXPosition`/`AXSize` writes. Before repositioning, you must read the current value of `kAXEnhancedUserInterfaceAttribute` from the **app** element, set it to `false`, perform the resize, then restore the original value. GlazeWM does this in a helper called `with_enhanced_ui_disabled`.

---

### Thread Affinity: The Most Important Constraint

`AXUIElement` objects are **not thread-safe**. The Accessibility API is built on top of Core Foundation and Objective-C, and all AX calls must be made on the **main thread** (the thread running `NSApplication`/`CFRunLoop`). Calling them from a background thread produces silent failures or crashes.

The design pattern to handle this:

1. **Store the `AXUIElement` in a thread-bound wrapper** that records which thread owns it (the event loop/main thread) and prevents direct access from any other thread.
2. **Use a dispatcher** — a mechanism to execute arbitrary closures on the main thread from any other thread.
3. Every window operation (`get position`, `set position`, `set size`, etc.) goes through the dispatcher: the calling thread submits a closure and **blocks** until the main thread executes it and returns the result.

Concretely, the dispatcher (on macOS) works via a `CFRunLoopSource`:

- A background thread enqueues a closure in an `mpsc` channel, then signals the run loop source and wakes the run loop.
- The run loop's callback (executing on the main thread) drains all pending closures.
- For synchronous calls, the background thread waits on a response channel with a timeout.

In any other language, the equivalent is: a thread-safe queue (or channel) of work items that the main thread drains in its event loop iteration. On macOS with Swift or Objective-C, `DispatchQueue.main.sync {}` does this implicitly.

---

## How GlazeWM Intercepts User-Initiated Window Moves

### Receiving Move Notifications

macOS does not have a single system-wide hook like Windows' `SetWinEventHook`. Instead, you must create a **per-process `AXObserver`** for every running application:

```
app launches
  → NSWorkspace notification: WorkspaceDidLaunchApplication
    → create AXObserver for that app's PID
      → register    AXWindowCreated on the app element
      → for each existing window:
          register AXWindowMoved, AXWindowResized, AXWindowMiniaturized, etc.
```

When the user moves a window, `AXWindowMoved` fires on the observer. When the user resizes it, `AXWindowResized` fires. Both are mapped to a `MovedOrResized` event. These observer callbacks run on the **main thread** (the `CFRunLoop` where the observer's run loop source was added).

**Key difference from Windows:** On Windows, `EVENT_SYSTEM_MOVESIZESTART` and `EVENT_SYSTEM_MOVESIZEEND` bracket the entire drag. macOS has no such events. `AXWindowMoved` fires **repeatedly** during the drag (once per position change) with no start/end demarcation. Your code must infer drag state on its own.

### Detecting Drag Start

Since there is no "start" notification, GlazeWM heuristically detects drag start on the first `AXWindowMoved` event:

1. Check that the mouse button is currently held down (`NSEvent.pressedMouseButtons()`, bit 0 = left button).
2. Check that the cursor position is within the window's current frame (expanded by ~40 pixels in all directions to account for the lag between when the user initiates a drag and when the first AX notification fires).
3. Verify no other window is already being dragged.

If all three conditions are true, a drag is recorded as having started.

### Detecting Drag End

Since there is no `MOVESIZEEND` equivalent, GlazeWM uses two complementary methods:

1. **`CGEventTap` on `LeftMouseUp`** — a system-wide low-level event tap (`CGEventTapCreate` at `kCGHIDEventTap`) fires on mouse button release before most AX notifications settle. This is the primary path.
2. **Polling inside `AXWindowMoved` handler** — on each move notification while a drag is recorded, check `NSEvent.pressedMouseButtons()`. If the left button is no longer down, treat the drag as ended. This is a safety net in case a `LeftMouseUp` was missed.

---

## How the Window Manager Maintains Window Position ("Fighting Back")

This is the core challenge: when the user moves a window, GlazeWM wants to snap it back to its tiling slot after the drag ends — but not during the drag (fighting the user's movements in real time creates an unusable experience).

The mechanism is a **deferred sync + active drag flag**.

### The Deferred Sync Queue (`PendingSync`)

Commands and event handlers never call the OS positioning API directly. They queue work:

- `queue_container_to_redraw(container)` — marks a container (and all its descendant windows) as needing `set_frame` called on next sync.
- `dequeue_container_from_redraw(container)` — removes a container from the redraw queue.

After every event is processed, if the queue is non-empty, `platform_sync` runs and calls `set_frame` for every queued window.

`reposition_window` (called per window during sync) has a special branch: **if the window has an active drag recorded, only the size is applied, never the position**. This prevents GlazeWM from teleporting a window the user is actively dragging.

### The Full Lifecycle

**Phase 1 — drag start:**
When drag is detected, GlazeWM sets `active_drag = Some(...)` on the window object with the initial position. Nothing is queued for redraw. No OS call happens.

**Phase 2 — during drag (window was tiling):**
On subsequent `AXWindowMoved` events, if the window has moved ≥ 10 pixels from its initial position and is a tiling window, GlazeWM **transiently promotes it to floating state**. This removes it from the tiling tree. Then it calls `dequeue_container_from_redraw` to ensure even if some ancestor gets queued, this window won't be repositioned mid-drag. If any sync runs, only size (not position) is applied.

**Phase 3 — drag end:**

- _Was tiling, dropped somewhere:_ GlazeWM reads the final cursor position, walks the container tree to find what container the cursor is over, inserts the window into the tiling tree at that slot (determining left/right/top/bottom quadrant from where in the target container the cursor landed). Then `queue_container_to_redraw` is called on the parent — `platform_sync` fires, `set_frame` is called with the position calculated from the tiling layout, and the window **snaps to its tiling slot**.
- _Was already floating:_ GlazeWM records the final window frame as the new floating placement. No redraw is queued. The window stays exactly where the user dropped it.
- _Was tiling, being resized (not moved):_ GlazeWM reads the final width/height from the AX API and updates the window's proportional size in the tiling tree. Then `queue_container_to_redraw` fires. `set_frame` snaps the window back to the tiling grid position, but now with the new proportional width/height respected.

The **snap-back** is therefore: `clear active_drag flag → queue_container_to_redraw → platform_sync → set_frame`.

### Drag Lifecycle Diagram

```
User grabs window titlebar
    │
    ├─ AXWindowMoved fires → handle_window_moved_or_resized
    │   ├─ no active_drag yet
    │   ├─ is_drag_start = true (left-click down + cursor in frame+40px)
    │   └─ set_active_drag(Some(ActiveDrag { operation: None, initial_position: frame }))
    │       → NO redraw queued
    │
    ├─ AXWindowMoved fires again (window moved ≥10px)
    │   ├─ active_drag is Some → update_drag_state
    │   ├─ classify as Move (dimensions unchanged)
    │   ├─ distance >= 10px → update_window_state(Floating)
    │   └─ dequeue_container_from_redraw  ← GlazeWM stops fighting
    │       (platform_sync runs but reposition_window only calls resize())
    │
    ├─ ... more AXWindowMoved/Resized events (ignored if position == initial) ...
    │
    └─ CGEventTap LeftMouseUp fires → handle_mouse_move
        ├─ refresh frame from AX
        └─ handle_window_moved_or_resized_end
            ├─ if was tiling: drop_as_tiling_window
            │   ├─ find target container at cursor
            │   ├─ move_container_within_tree
            │   └─ queue_container_to_redraw(target_parent)
            │       → platform_sync → set_frame snaps window to tiling slot
            └─ if was floating: update_floating_window_position
                └─ set_floating_placement(final_frame)
                    → no snap-back, window stays where dropped
```

---

## Considerations for Implementing This in Another Language

### 1. Permissions Must Be Checked at Startup

`AXIsProcessTrustedWithOptions` must return `true` before any AX API call will succeed. If not trusted, all reads return empty/error, and all writes silently fail. On macOS 14+, the permission dialog does not interrupt execution — you must poll or restart.

```objc
NSDictionary *options = @{ (__bridge id)kAXTrustedCheckOptionPrompt: @YES };
BOOL trusted = AXIsProcessTrustedWithOptions((__bridge CFDictionaryRef)options);
```

### 2. Per-Process Observer Management is Non-Trivial

You need a live inventory of running apps. `NSWorkspace` notifications give you launches and terminations, but you also need to enumerate all currently-running apps at startup and subscribe to their windows. Windows may appear on an app that is already running before your WM started.

For each app, you must:

- Create an `AXObserver` with that app's PID.
- Add its run loop source to your main thread's `CFRunLoop`.
- Register `AXWindowCreated` on the app element.
- Enumerate existing windows via `AXWindowsAttribute` and register per-window notifications on each.
- When a new window appears (`AXWindowCreated`), register on it immediately.
- When the app terminates, remove the observer to avoid stale AX calls.

### 3. AXUIElement is Not a Stable Identity

Unlike `HWND` (which is a stable integer for the window's entire lifetime), an `AXUIElement` can become invalid at any time (if the window is destroyed, or if the app crashes). Every AX call returns a `kAXErrorInvalidUIElement` code if the element is stale. You cannot cache an `AXUIElement` across significant time gaps and assume it is still valid. Build your internal window objects around a **stable identity** (e.g., `CGWindowID` from `kAXWindowIDAttribute`) and re-fetch the `AXUIElement` when needed, or always check error codes.

### 4. Design the Dispatcher First

Whether you use Swift, C#, Java, or Python — if your WM logic runs off the main thread (recommended, since blocking the main thread blocks the run loop), you need a dispatcher abstraction from day one. Every window operation must be callable from your WM thread but execute on the main thread.

In Swift:

```swift
DispatchQueue.main.sync { /* AX calls here */ }
```

In C# (when using NSApplication):

```csharp
NSApplication.SharedApplication.InvokeOnMainThread(() => { /* AX calls */ });
```

### 5. The `AXEnhancedUserInterface` Workaround is Mandatory for Certain Apps

Electron apps, Firefox, and LibreOffice all enable `AXEnhancedUserInterface` on their app element. Without disabling it before each `set_frame`, your position writes will be ignored or produce incorrect results. You must always check-and-disable-then-restore this attribute on the **app** `AXUIElement` (not the window element) for every reposition operation.

### 6. Set Size Before Position, Then Size Again

Always use the order: size → position → size. This is not documented by Apple but is empirically required for Electron and Gecko-based apps to respect both the new position and new size simultaneously.

### 7. The "No Start/End" Problem for Drags

Build a state machine:

- **Idle**: no drag in progress.
- **Dragging**: transition triggered by: `AXWindowMoved` + left mouse button down + cursor near window frame.
- **Ended**: transition triggered by: `CGEventTap` `kCGEventLeftMouseUp`, OR `AXWindowMoved` while drag state is active but left mouse button is now up.

Do not use AX notifications alone to determine drag end. They may continue firing briefly after mouse release. The `CGEventTap` on `LeftMouseUp` is the most reliable signal.

### 8. Snap-Back Strategy

- **Never fight the user in real time.** While a drag is in progress, do not call `set_frame`. Buffer any needed redraws.
- **On drag end:** Clear the drag state, decide the final tiling slot, then call `set_frame` once with the authoritative tiling position. From the user's perspective, on mouse release the window snaps to the nearest tiling slot.
- **Threshold before promoting to floating:** use a small movement threshold (~10px) before deciding a tiling window is being moved vs. a spurious micromove. This prevents accidental detiling from keyboard-triggered repositions.

### 9. `CGWindowID` for Identity, `AXUIElement` for Control

Obtain `CGWindowID` from the `AXUIElement` via `kAXWindowIDAttribute`. Use `CGWindowListCopyWindowInfo` with that ID to get screen-level information (bounds visible to the compositor, z-order, whether the window is onscreen). Use `AXUIElement` only for setting position/size/focus. This separation keeps your identity model stable even when the AX object is re-fetched.

### 10. Spaces (Virtual Desktops) are Read-Only

macOS Spaces cannot be programmatically rearranged the way GlazeWM workspaces can be on Windows. You can detect a Space change via `NSWorkspaceActiveSpaceDidChangeNotification`, and you can move a window to a Space via private SkyLight APIs, but the official AX API offers no Space management. Design your workspace model to treat Spaces as external constraints rather than fully managed resources.
