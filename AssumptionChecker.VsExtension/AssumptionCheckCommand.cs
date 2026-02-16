///// Command to open the Assumption Checker tool window /////

// == namespaces == //
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using AssumptionChecker.Core;

namespace AssumptionChecker.VsExtension
{
    [VisualStudioContribution]
    internal class AssumptionCheckCommand : Command
    {
        public override CommandConfiguration CommandConfiguration => new("%AssumptionCheckCommand.DisplayName%")
        {
            Placements = new[] { CommandPlacement.KnownPlacements.ToolsMenu },
            Icon = new(ImageMoniker.KnownValues.StatusInformation, IconSettings.None),
        };

        public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
        {
            // Open the tool window
            await this.Extensibility.Shell().ShowToolWindowAsync<AssumptionCheckerToolWindow>(activate: true, cancellationToken);
        }
    }
}
