///// command that opens the Assumption Checker tool window /////

// == namespaces == //
using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace AssumptionChecker.VsExtension
{
    internal sealed class AssumptionCheckerToolWindowCommand
    {
        // == must match the GUIDs and IDs in .vsct == //
        public static readonly Guid CommandSet = new("e8b4c2f1-7d3a-4a6e-9c5b-8f1d2e3a4b6c");
        public const int CommandId = 0x0100;

        private readonly AsyncPackage _package;

        private AssumptionCheckerToolWindowCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            _package = package;
            var menuCommandId = new CommandID(CommandSet, CommandId);
            var menuItem      = new MenuCommand(Execute, menuCommandId);
            commandService.AddCommand(menuItem);
        }

        // == called from Package.InitializeAsync == //
        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService
                                 ?? throw new InvalidOperationException("Cannot get command service");
            new AssumptionCheckerToolWindowCommand(package, commandService);
        }

        // == opens the tool window when the user clicks the menu item == //
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var window = _package.FindToolWindow(typeof(AssumptionCheckerToolWindow), 0, true);
            if (window?.Frame is IVsWindowFrame frame)
            {
                Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(frame.Show());
            }
        }
    }
}