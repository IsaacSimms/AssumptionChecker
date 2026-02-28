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

        // == clear field when focused. This prevent the raw key from ever being viewed by the user == //
        private void ApiKeyBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ApiKeyBox.Text = string.Empty;
        }

        // == save API key only when new one is entered. Maintains blur == //
        private void ApiKeyBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var newKey = ApiKeyBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(newKey)) _viewModel.Settings.ApiKey = newKey;

            BindingOperations.ClearBinding(ApiKeyBox, System.Windows.Controls.TextBox.TextProperty);
            ApiKeyBox.Text = _viewModel.Settings.MaskedApiKey;
        }
    }
}