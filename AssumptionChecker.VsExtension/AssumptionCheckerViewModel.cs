// <summary>
// ViewModel for the VS Extension tool window.
// Handles prompt analysis, model selection (OpenAI + Anthropic), and API key management.
// Settings panel calls Engine /settings endpoints to save/check API keys.
// </summary>

// == namespaces == //
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AssumptionChecker.Contracts;
using AssumptionChecker.Core;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using EnvDTE80;

namespace AssumptionChecker.VsExtension
{
    // == ViewModel: drives the tool window UI == //
    public class AssumptionCheckerViewModel : INotifyPropertyChanged
    {
        // == private fields == //
        private readonly IAssumptionCheckerService _service;
        private readonly string _engineBaseUrl;
        private string _promptText       = string.Empty;
        private string _resultText       = string.Empty;
        private string _selectedModel    = "gpt-4o-mini";
        private Visibility _isAnalyzing  = Visibility.Collapsed;
        private bool _canAnalyze         = true;

        // == settings fields == //
        private string _openAiApiKey     = string.Empty;
        private string _anthropicApiKey  = string.Empty;
        private bool _hasOpenAiKey;
        private bool _hasAnthropicKey;
        private bool _settingsExpanded;
        private string _settingsStatus   = string.Empty;

        // == constructor == //
        public AssumptionCheckerViewModel(IAssumptionCheckerService service, string engineBaseUrl)
        {
            _service        = service;
            _engineBaseUrl  = engineBaseUrl.TrimEnd('/');
            AnalyzeCommand  = new RelayCommand(async _ => await AnalyzeAsync(), _ => _canAnalyze);
            SaveOpenAiKeyCommand    = new RelayCommand(async _ => await SaveApiKeyAsync("openai", OpenAiApiKey));
            SaveAnthropicKeyCommand = new RelayCommand(async _ => await SaveApiKeyAsync("anthropic", AnthropicApiKey));

            _ = Task.Run(() => LoadProviderStatusAsync()); // fire-and-forget on startup
        }

        // == bindable properties == //
        public string PromptText
        {
            get => _promptText;
            set { _promptText = value; OnPropertyChanged(); }
        }

        public string ResultText
        {
            get => _resultText;
            set { _resultText = value; OnPropertyChanged(); }
        }

        public Visibility IsAnalyzing
        {
            get => _isAnalyzing;
            set { _isAnalyzing = value; OnPropertyChanged(); }
        }

        public string SelectedModel
        {
            get => _selectedModel;
            set { _selectedModel = value; OnPropertyChanged(); }
        }

        // == available models: OpenAI + Anthropic == //
        public List<string> AvailableModels { get; } = new List<string>
        {
            // OpenAI
            "gpt-4o-mini",
            "gpt-4o",
            "o1-mini",
            "o1",
            "gpt-5.2",
            "gpt-5-mini",
            // Anthropic
            "claude-sonnet-4-6",
            "claude-haiku-4-5",
            "claude-opus-4-6",
        };

        // == settings properties == //
        public string OpenAiApiKey
        {
            get => _openAiApiKey;
            set { _openAiApiKey = value; OnPropertyChanged(); }
        }

        public string AnthropicApiKey
        {
            get => _anthropicApiKey;
            set { _anthropicApiKey = value; OnPropertyChanged(); }
        }

        public bool HasOpenAiKey
        {
            get => _hasOpenAiKey;
            set { _hasOpenAiKey = value; OnPropertyChanged(); }
        }

        public bool HasAnthropicKey
        {
            get => _hasAnthropicKey;
            set { _hasAnthropicKey = value; OnPropertyChanged(); }
        }

        public bool SettingsExpanded
        {
            get => _settingsExpanded;
            set { _settingsExpanded = value; OnPropertyChanged(); }
        }

        public string SettingsStatus
        {
            get => _settingsStatus;
            set { _settingsStatus = value; OnPropertyChanged(); }
        }

        // == commands == //
        public ICommand AnalyzeCommand          { get; }
        public ICommand SaveOpenAiKeyCommand    { get; }
        public ICommand SaveAnthropicKeyCommand { get; }

        // == load provider key status from Engine == //
        private async Task LoadProviderStatusAsync()
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var json = await client.GetStringAsync($"{_engineBaseUrl}/settings/providers");
                HasOpenAiKey    = json.Contains("\"openai\":true") || json.Contains("\"openai\": true");
                HasAnthropicKey = json.Contains("\"anthropic\":true") || json.Contains("\"anthropic\": true");
            }
            catch
            {
                // Engine may not be ready yet; status will show as unconfigured
            }
        }

        // == save API key to Engine via POST /settings/apikey == //
        private async Task SaveApiKeyAsync(string provider, string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                SettingsStatus = $"Please enter a {provider} API key.";
                return;
            }

            try
            {
                SettingsStatus = "Saving...";
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                var body    = $"{{\"provider\":\"{provider}\",\"apiKey\":\"{EscapeJson(apiKey)}\"}}";
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{_engineBaseUrl}/settings/apikey", content);
                response.EnsureSuccessStatusCode();

                // update status
                if (provider == "openai")    { HasOpenAiKey = true;    OpenAiApiKey = string.Empty; }
                if (provider == "anthropic") { HasAnthropicKey = true; AnthropicApiKey = string.Empty; }
                SettingsStatus = $"{provider} key saved successfully.";
            }
            catch (Exception ex)
            {
                SettingsStatus = $"Error saving {provider} key: {ex.Message}";
            }
        }

        // == minimal JSON string escape == //
        private static string EscapeJson(string s) =>
            s.Replace("\\", "\\\\").Replace("\"", "\\\"");

        // == gathers context from all open documents in VS == //
        private List<FileContext> GatherOpenFileContexts()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var fileContexts = new List<FileContext>();

            if (!(Package.GetGlobalService(typeof(DTE)) is DTE2 dte))
                return fileContexts;

            foreach (Document doc in dte.Documents)
            {
                try
                {
                    var filePath = doc.FullName;
                    if (!File.Exists(filePath)) continue;

                    var content = File.ReadAllText(filePath);

                    if (content.Length > 10_000)
                        content = content.Substring(0, 10_000) + "\n// ... (truncated)";

                    fileContexts.Add(new FileContext
                    {
                        FilePath = filePath,
                        Content  = content
                    });
                }
                catch { continue; }
            }

            return fileContexts;
        }

        // == driver method for analyzing the prompt == //
        private async Task AnalyzeAsync()
        {
            if (string.IsNullOrWhiteSpace(PromptText))
            {
                ResultText = "ERROR: Please enter a prompt to analyze.";
                return;
            }

            _canAnalyze = false;
            IsAnalyzing = Visibility.Visible;
            ResultText  = string.Empty;

            try
            {
                // Gather file contexts on UI thread (DTE requires it)
                List<FileContext> fileContexts;
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                fileContexts = GatherOpenFileContexts();

                // Call the engine on a background thread
                var result = await Task.Run(() =>
                        _service.AnalyzeAsync(PromptText, maxAssumptions: 10, model: SelectedModel, fileContexts: fileContexts, cancellationToken: CancellationToken.None));

                ResultText = FormatResults(result);
            }
            catch (Exception ex)
            {
                ResultText = $"ERROR: {ex.Message}\n\nMake sure the Engine is running:\n  cd AssumptionChecker.Engine\n  dotnet run";
            }
            finally
            {
                IsAnalyzing = Visibility.Collapsed;
                _canAnalyze = true;
            }
        }

        // == format results == //
        private static string FormatResults(AnalyzeResponse response)
        {
            var output = new StringBuilder();
            output.AppendLine($"Found {response.Assumptions.Count} assumption(s)");
            output.AppendLine($"Model: {response.Metadata.ModelUsed}, Latency: {response.Metadata.LatencyMs}ms\n");

            foreach (var assumption in response.Assumptions)
            {
                output.AppendLine($"[{assumption.RiskLevel}] {assumption.AssumptionText}");
                output.AppendLine($"  Category: {assumption.Category}");
                output.AppendLine($"  Rationale: {assumption.Rationale}");
                if (!string.IsNullOrWhiteSpace(assumption.ClarifyingQuestion))
                    output.AppendLine($"  Ask: {assumption.ClarifyingQuestion}");
                output.AppendLine();
            }

            if (response.SuggestedPrompts.Count > 0)
            {
                output.AppendLine("SUGGESTED IMPROVED PROMPTS:");
                for (int i = 0; i < response.SuggestedPrompts.Count; i++)
                    output.AppendLine($"{i + 1}. {response.SuggestedPrompts[i]}");
            }

            return output.ToString();
        }

        // == INotifyPropertyChanged == //
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
