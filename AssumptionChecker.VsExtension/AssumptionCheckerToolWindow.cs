///// tool window pane — VS creates this, no source generator involved /////

// == namespaces == //
using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using AssumptionChecker.Core;

namespace AssumptionChecker.VsExtension
{
    [Guid("b7e1d3a9-2c4f-4a8e-b6d5-0c1e3f2a4b7e")]
    public class AssumptionCheckerToolWindow : ToolWindowPane
    {
        public AssumptionCheckerToolWindow() : base(null)
        {
            Caption = "Assumption Checker";

            // == create the WPF content with services from the Package == //
            var viewModel = new AssumptionCheckerViewModel(AssumptionCheckerPackage.CheckerService!, AssumptionCheckerPackage.EngineUrl);
            Content = new AssumptionCheckerControl(viewModel);
        }
    }
}
