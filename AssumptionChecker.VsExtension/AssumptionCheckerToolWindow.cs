///// Controls the tool window which handles UI for VS extension of Assumption Checker /////

// == namespaces == //
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.ToolWindows;
using Microsoft.VisualStudio.RpcContracts.RemoteUI;
using AssumptionChecker.Core;

namespace AssumptionChecker.VsExtension
{
    [VisualStudioContribution]

    // == defines the tool window == //
    internal class AssumptionCheckerToolWindow : ToolWindow
    {
        private readonly IAssumptionCheckerService _service; // Service to analyze assumptions, injected via constructor

        // == constructor with dependency injection == //
        public AssumptionCheckerToolWindow(VisualStudioExtensibility extensibility, IAssumptionCheckerService service) 
            : base(extensibility)
        {
            _service   = service;              // Store the injected service for later use in the tool window
            this.Title = "Assumption Checker"; // Set the title of the tool window
        }

        // == configure the tool window placement == //
        public override ToolWindowConfiguration ToolWindowConfiguration => new()
        {
            Placement = ToolWindowPlacement.DocumentWell,
        };

        // == creates the content of the tool window == //
        public override Task<IRemoteUserControl> GetContentAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IRemoteUserControl>(
                new AssumptionCheckerControl(new AssumptionCheckerData(_service, this.Extensibility)));
        }
    }
}
