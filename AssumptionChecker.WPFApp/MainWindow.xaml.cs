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

            _viewModel  = viewModel;
            DataContext  = _viewModel;

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

        // == reveal the real key for editing when the field is focused == //
        private void ApiKeyBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ApiKeyBox.SetBinding(System.Windows.Controls.TextBox.TextProperty, new Binding("Settings.ApiKey")
            {
                Source               = _viewModel,
                UpdateSourceTrigger  = UpdateSourceTrigger.PropertyChanged
            });
            ApiKeyBox.CaretIndex = ApiKeyBox.Text.Length;
        }

        // == restore the masked display when the field loses focus == //
        private void ApiKeyBox_LostFocus(object sender, RoutedEventArgs e)
        {
            BindingOperations.ClearBinding(ApiKeyBox, System.Windows.Controls.TextBox.TextProperty);
            ApiKeyBox.Text = _viewModel.Settings.MaskedApiKey;
        }
    }
}