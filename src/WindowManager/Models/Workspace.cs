namespace WindowManager.Models
{
    /// <summary>
    /// Represents a named workspace tab that holds a managed window.
    /// </summary>
    public class Workspace
    {
        /// <summary>Gets or sets the user-defined workspace name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the window managed by this workspace.
        /// <para>A single window is supported in the MVP; future phases will extend this to a list.</para>
        /// </summary>
        public ManagedWindow? Window { get; set; }
    }
}
