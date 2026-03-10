///// application entry point: configures DI and launches the main window /////

// == namespaces == //
using System.Diagnostics;
using System.IO;
using System.Net.Http;
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
        // == engine URL is an internal constant — not user-configurable == //
        private const string EngineUrl = "http://localhost:5046";
        private Process? _engineProcess;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // == load persisted settings == //
            var appSettingsService = new AppSettingsService();
            var appSettings        = appSettingsService.Load();

            // == start the engine if it isn't already running == //
            EnsureEngineRunning(EngineUrl);

            // == build the DI container == //
            var services = new ServiceCollection();
            services.AddAssumptionChecker(EngineUrl, timeoutSeconds: 120);
            services.AddSingleton<IAppSettingsService>(appSettingsService);
            services.AddSingleton(appSettingsService);

            var provider = services.BuildServiceProvider();

            // == resolve services and build view models == //
            var checkerService = provider.GetRequiredService<IAssumptionCheckerService>();
            var secureSettings = provider.GetRequiredService<ISecureSettingsManager>();

            var settingsVm = new SettingsViewModel(secureSettings, appSettingsService, EngineUrl);
            var mainVm     = new MainViewModel(checkerService, appSettingsService, settingsVm);

            new MainWindow(mainVm).Show();
        }

        // == handle WPF app closing logic == //
        protected override void OnExit(ExitEventArgs e)
        {
            // == shut the engine down cleanly when the WPF app closes == //
            try { _engineProcess?.Kill(entireProcessTree: true); } catch { }
            base.OnExit(e);
        }

        // == start the engine process if it is not already responding == // // important for WPF form startup... engine must start too
        private void EnsureEngineRunning(string engineUrl)
        {
            if (IsEngineAlreadyRunning(engineUrl)) return;

            // look for the engine executable next to the WPF app
            var engineExe = Path.Combine(
                AppContext.BaseDirectory,
                "Engine",
                "AssumptionChecker.Engine.exe");

            if (!File.Exists(engineExe))
            {
                MessageBox.Show(
                    $"Engine not found at:\n{engineExe}\n\nPlease reinstall the application.",
                    "Assumption Checker", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            _engineProcess = new Process
            {
                StartInfo = new ProcessStartInfo(engineExe)
                {
                    UseShellExecute  = false,
                    CreateNoWindow   = true  // runs silently in the background
                }
            };
            _engineProcess.Start();

            // == wait up to 10 seconds for the engine to be ready == //
            WaitForEngine(engineUrl, timeoutSeconds: 10);
        }

        // == ping /health to check if the engine is already up == //
        private static bool IsEngineAlreadyRunning(string engineUrl)
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };
                var response = client.GetAsync($"{engineUrl}/health").GetAwaiter().GetResult();
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        // == poll /health until the engine responds or the timeout expires == //
        private static void WaitForEngine(string engineUrl, int timeoutSeconds)
        {
            var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            while (DateTime.UtcNow < deadline)
            {
                if (IsEngineAlreadyRunning(engineUrl)) return;
                System.Threading.Thread.Sleep(500);
            }
        }
    }
}
