///// used by VS extension to get into engine/API service /////

// == namespaces == //
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Extensibility;
using AssumptionChecker.Core;

namespace AssumptionChecker.VsExtension
{
    [VisualStudioContribution] // marks this class as the entry point for the VS extension
    internal class ExtensionEntrypoint : Extension
    {
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

            serviceCollection.AddAssumptionChecker(engineUrl);
        }
    }
}
