using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using WindowManager.Services;
using WindowManager.ViewModels;
using WindowManager.Views;

namespace WindowManager
{
    /// <summary>
    /// Application entry point and global resource host.
    /// Configures the dependency injection container and launches the main window.
    /// </summary>
    public partial class App : Application
    {
        private IServiceProvider _serviceProvider = null!;

        /// <summary>
        /// Configures the DI container and shows the main window.
        /// </summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        /// <summary>
        /// Disposes the service provider when the application exits.
        /// </summary>
        protected override void OnExit(ExitEventArgs e)
        {
            if (_serviceProvider is IDisposable disposable)
                disposable.Dispose();

            base.OnExit(e);
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // Services
            services.AddSingleton<WindowService>();
            services.AddSingleton<WorkspaceManager>();

            // ViewModels
            services.AddSingleton<MainViewModel>();

            // Views
            services.AddSingleton<MainWindow>();
        }
    }
}
