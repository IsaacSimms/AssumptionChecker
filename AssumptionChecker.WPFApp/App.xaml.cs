///// application entry point: configures DI and launches the main window /////

// == namespaces == //
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using AssumptionChecker.Core;
using AssumptionChecker.WPFApp.Services;
using AssumptionChecker.WPFApp.ViewModels;

namespace AssumptionChecker.WPFApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // == load persisted settings == //
            var appSettingsService = new AppSettingsService();
            var appSettings       = appSettingsService.Load();

            // == build the DI container using the Core extension == //
            var services = new ServiceCollection();
            services.AddAssumptionChecker(appSettings.EngineUrl, timeoutSeconds: 120);
            services.AddSingleton(appSettingsService);

            var provider = services.BuildServiceProvider();

            // == resolve services and build view models == //
            var checkerService = provider.GetRequiredService<IAssumptionCheckerService>();
            var secureSettings = provider.GetRequiredService<ISecureSettingsManager>();

            var settingsVm = new SettingsViewModel(secureSettings, appSettingsService);
            var mainVm     = new MainViewModel(checkerService, appSettingsService, settingsVm);

            // == show the main window == //
            var window = new MainWindow(mainVm);
            window.Show();
        }
    }
}
