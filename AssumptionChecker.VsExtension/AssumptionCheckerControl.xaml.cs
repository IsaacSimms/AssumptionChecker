using System.Windows.Controls;
using System.Windows.Input;

namespace AssumptionChecker.VsExtension
{
    public partial class AssumptionCheckerControl : UserControl
    {
        public AssumptionCheckerControl(AssumptionCheckerViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        // == Enter submits; Shift+Enter inserts a newline == //
        private void PromptTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
            {
                var vm = DataContext as AssumptionCheckerViewModel;
                if (vm?.AnalyzeCommand.CanExecute(null) == true)
                    vm.AnalyzeCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}