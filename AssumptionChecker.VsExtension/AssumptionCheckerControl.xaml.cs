using System.Windows.Controls;

namespace AssumptionChecker.VsExtension
{
    public partial class AssumptionCheckerControl : UserControl
    {
        public AssumptionCheckerControl(AssumptionCheckerViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}