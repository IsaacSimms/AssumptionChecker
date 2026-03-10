using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using AssumptionChecker.WPFApp.ViewModels;

namespace AssumptionChecker.WPFApp
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;
            DataContext = _viewModel;

            // auto-scroll when new messages are added
            _viewModel.MessageAdded += () =>
                Dispatcher.InvokeAsync(() => ChatScrollViewer.ScrollToEnd(),
                    System.Windows.Threading.DispatcherPriority.Background);
        }

        // == Enter sends the message, Shift+Enter inserts a new line == //
        private void InputBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                if (_viewModel.SendCommand.CanExecute(null))
                    _viewModel.SendCommand.Execute(null);

                e.Handled = true;
            }
        }

        // == Anthropic key: clear on focus, update on blur == //
        private void AnthropicKeyBox_GotFocus(object sender, RoutedEventArgs e)
        {
            AnthropicKeyBox.Text = string.Empty;
        }

        private void AnthropicKeyBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var newKey = AnthropicKeyBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(newKey)) _viewModel.Settings.AnthropicApiKey = newKey;

            BindingOperations.ClearBinding(AnthropicKeyBox, System.Windows.Controls.TextBox.TextProperty);
            AnthropicKeyBox.Text = _viewModel.Settings.MaskedAnthropicApiKey;
        }

        // == OpenAI key: clear on focus, update on blur == //
        private void OpenAiKeyBox_GotFocus(object sender, RoutedEventArgs e)
        {
            OpenAiKeyBox.Text = string.Empty;
        }

        private void OpenAiKeyBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var newKey = OpenAiKeyBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(newKey)) _viewModel.Settings.OpenAiApiKey = newKey;

            BindingOperations.ClearBinding(OpenAiKeyBox, System.Windows.Controls.TextBox.TextProperty);
            OpenAiKeyBox.Text = _viewModel.Settings.MaskedOpenAiApiKey;
        }
    }
}