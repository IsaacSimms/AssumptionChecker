///// used by VS extension to get into engine/API service /////

// == namespaces == //
using AssumptionChecker.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Extensibility;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AssumptionChecker.VsExtension
{
    [VisualStudioContribution]                  // marks this class as the entry point for the VS extension
    internal class ExtensionEntrypoint : Extension
    {
        private static Process? _engineProcess;           // tracks engine process

        // == For hanlding child processes and ensuring cleanup on exit == //
        private static IntPtr   _jobHandle = IntPtr.Zero; // job object to ensure child processes are cleaned up

        // == P/Invoke API declarations for Windows Job Objects == //
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string? lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetInformationJobObject(IntPtr hJob, int jobObjectInfoClass, ref JobObjectExtendedLimitInfo lpInfo, int cbInfoLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [StructLayout(LayoutKind.Sequential)]
        private struct JobObjectBasicLimitInfo
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public uint LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public IntPtr Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IoCounters
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JobObjectExtendedLimitInfo
        {
            public JobObjectBasicLimitInfo BasicLimitInformation;
            public IoCounters IoInfo;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryUsed;
            public UIntPtr PeakJobMemoryUsed;
        }

        private const int JobObjectExtendedLimitInformationClass = 9;
        private const uint KillOnJobClose                        = 0x00002000; // OS kills child processes when job handle is closed

        // == ensureing cleanup on exit complete == //

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

            EnsureEngineIsRunning(engineUrl); // Auto-start the Engine if it's not already running

            serviceCollection.AddAssumptionChecker(engineUrl);
        }


        // == Engine management == //
        private static void EnsureEngineIsRunning(string engineUrl)
        {
            // Check if Engine is already running
            if (IsEngineRunning(engineUrl))
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

            WaitForEngineReady(engineUrl); // Wait for the Engine to be ready before proceeding
        }

        // == make sure engine is running == //
        private static void WaitForEngineReady(string engineUrl, int maxWaitMs = 5000)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < maxWaitMs)
            {
                if (IsEngineRunning(engineUrl))
                    return;
                Thread.Sleep(500);
            }
        }

        // == Health check for Engine == //
        private static bool IsEngineRunning(string engineUrl)
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                using var request = new HttpRequestMessage(HttpMethod.Get, $"{engineUrl}/health");
                using var response = client.Send(request);
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
            if (disposing)
            {
                if (_jobHandle != IntPtr.Zero)
                {
                    CloseHandle(_jobHandle);
                    _jobHandle = IntPtr.Zero;
                }

                // Clean up: stop the Engine when VS closes
                if (disposing && _engineProcess != null && !_engineProcess.HasExited)
                {
                    _engineProcess.Kill();
                    _engineProcess.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}
