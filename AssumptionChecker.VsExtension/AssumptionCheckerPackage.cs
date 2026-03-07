///// VS loads this class directly — no Main method, no source generator /////

// == namespaces == //
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using AssumptionChecker.Core;
using Microsoft.Extensions.DependencyInjection;
using Task = System.Threading.Tasks.Task;

// == Win32 job object: kills child processes when VS exits for any reason == //
internal static class NativeMethods
{
    internal const int  JobObjectExtendedLimitInformation = 9;
    internal const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000;

    [StructLayout(LayoutKind.Sequential)]
    internal struct JOBOBJECT_BASIC_LIMIT_INFORMATION
    {
        public long  PerProcessUserTimeLimit;
        public long  PerJobUserTimeLimit;
        public uint  LimitFlags;
        public UIntPtr MinimumWorkingSetSize;
        public UIntPtr MaximumWorkingSetSize;
        public uint  ActiveProcessLimit;
        public UIntPtr Affinity;
        public uint  PriorityClass;
        public uint  SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct IO_COUNTERS
    {
        public ulong ReadOperationCount, WriteOperationCount, OtherOperationCount;
        public ulong ReadTransferCount,  WriteTransferCount,  OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
    {
        public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
        public IO_COUNTERS                        IoInfo;
        public UIntPtr ProcessMemoryLimit, JobMemoryLimit, PeakProcessMemoryUsed, PeakJobMemoryUsed;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string? lpName);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool SetInformationJobObject(IntPtr hJob, int infoClass, IntPtr lpInfo, uint cbInfoLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool CloseHandle(IntPtr hObject);
}

namespace AssumptionChecker.VsExtension
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(AssumptionCheckerToolWindow),
        Style = VsDockStyle.Tabbed,
        Window = EnvDTE.Constants.vsWindowKindOutput)]
    [ProvideBindingPath]
    public sealed class AssumptionCheckerPackage : AsyncPackage
    {
        // == constants == //
        public const string PackageGuidString = "9a3d7b1e-4f2c-4e8a-b5d6-1c0e3f2a4b7d";

        // == shared service instance (accessible by tool window) == //
        internal static IAssumptionCheckerService? CheckerService { get; private set; }

        // == engine process management == //
        private static Process? _engineProcess;
        private static IntPtr   _engineJobHandle = IntPtr.Zero; // job object ties engine lifetime to this process

        // == package initialization (VS calls this — no Main needed) == //
        protected override async Task InitializeAsync(
            CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);

            var engineUrl = Environment.GetEnvironmentVariable("ASSUMPTION_CHECKER_ENGINE_URL")
                            ?? "http://localhost:5046";

            // Engine startup: fire-and-forget so a timeout never blocks command registration
            _ = Task.Run(() =>
            {
                try { EnsureEngineIsRunning(engineUrl); }
                catch { /* non-fatal: extension still works once engine is manually started */ }
            }, cancellationToken);

            // Service registration: guard against DI failures
            try
            {
                var services = new ServiceCollection();
                services.AddAssumptionChecker(engineUrl);
                var provider = services.BuildServiceProvider();
                CheckerService = provider.GetRequiredService<IAssumptionCheckerService>();
            }
            catch (Exception ex)
            {
                // Log and continue — command must still be registered
                Debug.WriteLine($"[AssumptionChecker] Service init failed: {ex.Message}");
            }

            // Always register the command, regardless of engine/service state
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await AssumptionCheckerToolWindowCommand.InitializeAsync(this);
        }

        // == engine management (moved from ExtensionEntrypoint) == //
        private static void EnsureEngineIsRunning(string engineUrl)
        {
            if (IsEngineRunning(engineUrl))
                return;

            var extensionDir = Path.GetDirectoryName(typeof(AssumptionCheckerPackage).Assembly.Location);
            var enginePath   = Path.Combine(extensionDir!, "Engine", "AssumptionChecker.Engine.exe");

            if (!File.Exists(enginePath))
            {
                enginePath = Path.Combine(extensionDir!, "..", "..", "..", "..",
                    "AssumptionChecker.Engine", "bin", "Debug", "net8.0", "AssumptionChecker.Engine.exe");
                if (!File.Exists(enginePath))
                    return;
            }

            _engineProcess = Process.Start(new ProcessStartInfo
            {
                FileName               = enginePath,
                UseShellExecute        = false,
                CreateNoWindow         = true,
                RedirectStandardOutput = true,
                RedirectStandardError  = true
            });

            // Attach engine to a job object so the OS kills it if VS exits for any reason
            AssignEngineToJobObject(_engineProcess);

            WaitForEngineReady(engineUrl);
        }

        // == creates a job object and assigns the engine process to it so the OS kills it when VS closes == //
        private static void AssignEngineToJobObject(Process? process)
        {
            if (process == null) return;
            try
            {
                _engineJobHandle = NativeMethods.CreateJobObject(IntPtr.Zero, null);
                if (_engineJobHandle == IntPtr.Zero) return;

                // Configure job: kill all assigned processes when the last handle is closed
                var info = new NativeMethods.JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
                info.BasicLimitInformation.LimitFlags = NativeMethods.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;

                int size = Marshal.SizeOf(typeof(NativeMethods.JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
                var ptr  = Marshal.AllocHGlobal(size);
                try
                {
                    Marshal.StructureToPtr(info, ptr, false);
                    NativeMethods.SetInformationJobObject(_engineJobHandle,
                        NativeMethods.JobObjectExtendedLimitInformation, ptr, (uint)size);
                }
                finally { Marshal.FreeHGlobal(ptr); }

                NativeMethods.AssignProcessToJobObject(_engineJobHandle, process.Handle);
            }
            catch { /* non-fatal: Dispose() kill is the fallback */ }
        }

        private static void WaitForEngineReady(string engineUrl, int maxWaitMs = 5000)
        {
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < maxWaitMs)
            {
                if (IsEngineRunning(engineUrl))
                    return;
                Thread.Sleep(500);
            }
        }

        private static bool IsEngineRunning(string engineUrl)
        {
            try
            {
                using var client  = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                using var request = new HttpRequestMessage(HttpMethod.Get, $"{engineUrl}/health");
                using var response = client.SendAsync(request).GetAwaiter().GetResult();
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        // == cleanup == //
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Explicit kill for graceful shutdown (job object handles abrupt termination)
                if (_engineProcess != null && !_engineProcess.HasExited)
                {
                    _engineProcess.Kill();
                    _engineProcess.Dispose();
                }

                if (_engineJobHandle != IntPtr.Zero)
                {
                    NativeMethods.CloseHandle(_engineJobHandle); // releasing last handle → OS kills engine
                    _engineJobHandle = IntPtr.Zero;
                }
            }
            base.Dispose(disposing);
        }
    }
}
