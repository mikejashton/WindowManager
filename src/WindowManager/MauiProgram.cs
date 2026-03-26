using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using WindowManager.Abstractions.Services;
using WindowManager.Services;
using WindowManager.ViewModels;
using WindowManager.Views;
#if WINDOWS
using WindowManager.Windows.Services;
#elif MACCATALYST
using WindowManager.MacOS.Services;
#endif

namespace WindowManager
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder.UseMauiApp<App>();

            // Platform-specific window service
#if WINDOWS
            builder.Services.AddSingleton<IWindowService, WindowsWindowService>();
#elif MACCATALYST
            builder.Services.AddSingleton<IWindowService, MacWindowService>();
#endif

            // Services
            builder.Services.AddSingleton<WorkspaceManager>();

            // ViewModels
            builder.Services.AddSingleton<MainViewModel>();

            // Views
            builder.Services.AddSingleton<MainWindow>();

            return builder.Build();
        }
    }
}
