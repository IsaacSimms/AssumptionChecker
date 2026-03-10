///// main view model: drives the chat UI and navigation /////

// == namespaces == //
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Input;
using AssumptionChecker.Contracts;
using AssumptionChecker.Core;
using AssumptionChecker.WPFApp.Models;
using AssumptionChecker.WPFApp.Services;

namespace AssumptionChecker.WPFApp.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        // == private fields == //
        private readonly IAssumptionCheckerService _service;       // injected service for calling the engine
        private readonly IAppSettingsService _appSettingsService;  // uses interface
        private string   _inputText       = string.Empty;
        private bool     _isProcessing;
        private bool     _isSettingsVisible;

        // == constructor == //
        public MainViewModel(
            IAssumptionCheckerService service,            // injected service for calling the engine
            IAppSettingsService appSettingsService,       // uses interface
            SettingsViewModel settingsViewModel)
        {
            _service            = service;
            _appSettingsService = appSettingsService;
            Settings            = settingsViewModel;

            // == commands == //
            SendCommand                = new RelayCommand(async _ => await SendAsync(), _ => CanSend());
            NewChatCommand             = new RelayCommand(_ => ClearChat());
            NavigateToSettingsCommand  = new RelayCommand(_ => IsSettingsVisible = true);
            LoadSuggestedPromptCommand = new RelayCommand(p => LoadSuggestedPrompt(p as string));

            // == start with a fresh chat on launch == //
            ClearChat();
        }

        // == bindable properties == //
        public ObservableCollection<ChatMessage> Messages { get; } = new();

        public SettingsViewModel Settings { get; }

        public string InputText
        {
            get => _inputText;
            set => SetProperty(ref _inputText, value);
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        public bool IsSettingsVisible
        {
            get => _isSettingsVisible;
            set => SetProperty(ref _isSettingsVisible, value);
        }

        // == commands == //
        public ICommand SendCommand                { get; }
        public ICommand NewChatCommand             { get; }
        public ICommand NavigateToSettingsCommand  { get; }
        public ICommand LoadSuggestedPromptCommand { get; }

        // == event raised when a new message is added (for auto-scroll) == //
        public event Action? MessageAdded;

        // == can-execute logic == //
        private bool CanSend() => !_isProcessing && !string.IsNullOrWhiteSpace(_inputText);

        // == clear the chat and show welcome message == //
        private void ClearChat()
        {
            IsSettingsVisible = false;
            Messages.Clear();
            Messages.Add(new ChatMessage
            {
                Role    = "Assumption Checker",
                Content = "Ready to analyze your next prompt!"
            });
            MessageAdded?.Invoke();
        }

        // == load a suggested prompt into the input field == //
        private void LoadSuggestedPrompt(string? prompt)
        {
            if (!string.IsNullOrWhiteSpace(prompt))
                InputText = prompt;
        }

        // == send the current prompt to the engine for analysis == //
        private async Task SendAsync()
        {
            if (string.IsNullOrWhiteSpace(InputText)) return;

            var userPrompt = InputText.Trim();
            InputText      = string.Empty;
            IsProcessing   = true;

            // add user message to the chat
            Messages.Add(new ChatMessage { Role = "User", Content = userPrompt });
            MessageAdded?.Invoke();

            // add a thinking placeholder
            var thinking = new ChatMessage
            {
                Role       = "Assumption Checker",
                Content    = "Analyzing your prompt...",
                IsThinking = true
            };
            Messages.Add(thinking);
            MessageAdded?.Invoke();

            try
            {
                var settings = _appSettingsService.Load();

                // call the engine on a background thread
                var result = await Task.Run(() =>
                    _service.AnalyzeAsync(userPrompt, settings.MaxAssumptions, settings.Model, cancellationToken: CancellationToken.None));

                // replace the thinking message with the real response
                Messages.Remove(thinking);
                Messages.Add(new ChatMessage
                {
                    Role             = "Assumption Checker",
                    Content          = FormatResults(result),
                    SuggestedPrompts = result.SuggestedPrompts ?? new()
                });
            }
            catch (HttpRequestException ex) when (ex.StatusCode == null)
            {
                Messages.Remove(thinking);
                Messages.Add(new ChatMessage
                {
                    Role    = "Assumption Checker",
                    Content = "⚠ Could not reach the Engine.\n\n" +
                              "Make sure it is running:\n" +
                              "  cd AssumptionChecker.Engine\n" +
                              "  dotnet run"
                });
            }
            
            catch (HttpRequestException ex)
            {
                Messages.Remove(thinking);
                Messages.Add(new ChatMessage
                {
                    Role    = "Assumption Checker",
                    Content = $"⚠ Error: Engine returned with error {ex.StatusCode}\n\n" +
                              "Check the Engine's console for details." +
                              "Please verify API key in settings."

                });
            }
            finally
            {
                IsProcessing = false;
                MessageAdded?.Invoke();
            }
        }

        // == format an AnalyzeResponse into readable plain text == //
        private static string FormatResults(AnalyzeResponse response)
        {
            // header with metadata
            var sb = new StringBuilder();
            sb.AppendLine($"Found {response.Assumptions.Count} assumption(s)");
            sb.AppendLine($"Model: {response.Metadata.ModelUsed}  •  Latency: {response.Metadata.LatencyMs}ms");
            sb.AppendLine(new string('─', 40));
            sb.AppendLine();

            // assumptions details
            foreach (var a in response.Assumptions)
            {
                var icon = a.RiskLevel switch
                {
                    RiskLevel.Low    => "🟢",
                    RiskLevel.Medium => "🟡",
                    RiskLevel.High   => "🔴",
                    _                => "⚪"
                };

                sb.AppendLine($"{icon} [{a.RiskLevel.ToString().ToUpper()}]  {a.AssumptionText}");
                sb.AppendLine($"    Category:    {a.Category}");
                sb.AppendLine($"    Rationale:   {a.Rationale}");
                if (!string.IsNullOrWhiteSpace(a.ClarifyingQuestion))
                    sb.AppendLine($"    Ask:         {a.ClarifyingQuestion}");
                sb.AppendLine($"    Confidence:  {a.Confidence:P0}");
                sb.AppendLine();
            }

            // clarifying questions summary
            var questions = response.Assumptions
                .Where(a => !string.IsNullOrWhiteSpace(a.ClarifyingQuestion))
                .Select(a => $"  • {a.ClarifyingQuestion}")
                .ToList();

            // only show the section if there are questions
            if (questions.Count > 0)
            {
                sb.AppendLine("❓ Clarifying Questions");
                foreach (var q in questions) sb.AppendLine(q);
                sb.AppendLine();
            }

            // suggested prompts note
            if (response.SuggestedPrompts.Count > 0)
            {
                sb.AppendLine("✨ Suggested Improved Prompts");
                sb.AppendLine("Click a suggestion below to load it into the input:");
            }

            return sb.ToString().TrimEnd();
        }
    }
}