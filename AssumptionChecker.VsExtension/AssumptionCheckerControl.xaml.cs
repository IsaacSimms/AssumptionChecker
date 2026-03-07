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

        // == PasswordBox doesn't support binding, so we relay via code-behind == //
        private void OpenAiKeyBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is AssumptionCheckerViewModel vm)
                vm.OpenAiApiKey = ((PasswordBox)sender).Password;
        }

        private void AnthropicKeyBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is AssumptionCheckerViewModel vm)
                vm.AnthropicApiKey = ((PasswordBox)sender).Password;
        }
    }
}