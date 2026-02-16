///// used by VS extension to get into engine/API service /////

// == namespaces == //
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Extensibility;
using AssumptionChecker.Core;

namespace AssumptionChecker.VsExtension
{
    [VisualStudioContribution]                  // marks this class as the entry point for the VS extension
    internal class ExtensionEntrypoint : Extension
    {
        private static Process? _engineProcess; // tracks engine process

        // == extension metadata == //
        public override ExtensionConfiguration ExtensionConfiguration => new()
        {
            // metadata that describes the extension in the VS marketplace and UI
            Metadata = new(
                id:            "AssumptionChecker.VsExtension",
                version:       ExtensionAssemblyVersion,
                publisherName: "IsaacSimms",
                displayName:   "Assumption Checker for Copilot",
                description:   "Analyzes Copilot prompts for hidden assumptions and suggests improved alternatives.")
        };

        // == InitializeServices == //
        protected override void InitializeServices(IServiceCollection serviceCollection)
        {
            base.InitializeServices(serviceCollection); // call base method to ensure any default services are registered

            // Read engine URL from environment variable or use localhost default
            // Users can set this via: setx ASSUMPTION_CHECKER_ENGINE_URL "http://localhost:5046"
            var engineUrl = Environment.GetEnvironmentVariable("ASSUMPTION_CHECKER_ENGINE_URL") 
                            ?? "http://localhost:5046";

            EnsureEngineIsRunning(); // Auto-start the Engine if it's not already running

            serviceCollection.AddAssumptionChecker(engineUrl);
        }


        // == Engine management == //
        private static void EnsureEngineIsRunning()
        {
            // Check if Engine is already running
            if (IsEngineRunning())
                return;

            // Get the Engine executable path (bundled with the extension)
            var extensionDir = Path.GetDirectoryName(typeof(ExtensionEntrypoint).Assembly.Location);
            var enginePath   = Path.Combine(extensionDir!, "Engine", "AssumptionChecker.Engine.exe");

            if (!File.Exists(enginePath))
            {
                // Fallback: try to find it in the solution (dev scenario)
                enginePath = Path.Combine(extensionDir!, "..", "..", "..", "..", "AssumptionChecker.Engine", "bin", "Debug", "net8.0", "AssumptionChecker.Engine.exe");
                
                if (!File.Exists(enginePath))
                    return; // Can't auto-start, user must start manually
            }

            // Start the Engine as a background process
            _engineProcess = Process.Start(new ProcessStartInfo
            {
                FileName = enginePath,
                UseShellExecute        = false,
                CreateNoWindow         = true, // Run in background
                RedirectStandardOutput = true,
                RedirectStandardError  = true
            });

            // Give it a moment to start listening
            Thread.Sleep(2000);
        }

        private static bool IsEngineRunning()
        {
            // is engine API responsive
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                var response = client.GetAsync("http://localhost:5046/health").GetAwaiter().GetResult();
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // == Cleanup == //
        protected override void Dispose(bool disposing)
        {
            // Clean up: stop the Engine when VS closes
            if (disposing && _engineProcess != null && !_engineProcess.HasExited)
            {
                _engineProcess.Kill();
                _engineProcess.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
