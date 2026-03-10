///// settings view model: manages API keys (OpenAI + Anthropic), engine URL, and preferences /////

// == namespaces == //
using System.Net.Http;
using System.Text;
using System.Windows.Input;
using AssumptionChecker.Core;
using AssumptionChecker.WPFApp.Models;
using AssumptionChecker.WPFApp.Services;

namespace AssumptionChecker.WPFApp.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        // == private fields == //
        private readonly ISecureSettingsManager _secureSettings;
        private readonly IAppSettingsService    _appSettingsService;
        private readonly string                _engineBaseUrl;
        private string _openAiApiKey    = string.Empty;
        private string _anthropicApiKey = string.Empty;
        private int    _maxAssumptions  = 10;
        private string _statusMessage   = string.Empty;
        private string _model           = "claude-haiku-4-5";

        // == constructor == //
        public SettingsViewModel(ISecureSettingsManager secureSettings, IAppSettingsService appSettingsService, string engineBaseUrl)
        {
            _secureSettings     = secureSettings;
            _appSettingsService = appSettingsService;
            _engineBaseUrl      = engineBaseUrl.TrimEnd('/');

            SaveCommand = new RelayCommand(async _ => await SaveAsync());

            LoadSettings();
        }

        // == bindable properties: OpenAI API key == //
        public string OpenAiApiKey
        {
            get => _openAiApiKey;
            set
            {
                if (SetProperty(ref _openAiApiKey, value))
                    OnPropertyChanged(nameof(MaskedOpenAiApiKey));
            }
        }

        public string MaskedOpenAiApiKey => _openAiApiKey.Length > 4
            ? new string('•', _openAiApiKey.Length - 4) + _openAiApiKey[^4..]
            : new string('•', _openAiApiKey.Length);

        // == bindable properties: Anthropic API key == //
        public string AnthropicApiKey
        {
            get => _anthropicApiKey;
            set
            {
                if (SetProperty(ref _anthropicApiKey, value))
                    OnPropertyChanged(nameof(MaskedAnthropicApiKey));
            }
        }

        public string MaskedAnthropicApiKey => _anthropicApiKey.Length > 4
            ? new string('•', _anthropicApiKey.Length - 4) + _anthropicApiKey[^4..]
            : new string('•', _anthropicApiKey.Length);

        // == available model options (Anthropic + OpenAI) == //
        public List<string> AvailableModels { get; } =
            [
                // Anthropic
                "claude-haiku-4-5",
                "claude-sonnet-4-6",
                "claude-opus-4-6",
                // OpenAI
                "gpt-4o-mini",
                "gpt-4o",
                "gpt-4.1",
                "gpt-4.1-mini",
                "gpt-4.1-nano",
                "o3-mini",
                "gpt-5.1",
                "gpt-5.2",
                "gpt-5.1-Codex"
            ];

        public string Model
        {
            get => _model;
            set => SetProperty(ref _model, value);
        }

        // == max assumptions clamped between 1 and 50 == //
        public int MaxAssumptions
        {
            get => _maxAssumptions;
            set => SetProperty(ref _maxAssumptions, Math.Clamp(value, 1, 50));
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // == commands == //
        public ICommand SaveCommand { get; }

        // == load settings from disk == //
        private void LoadSettings()
        {
            var openAiKey = _secureSettings.GetApiKey("openai");
            if (!string.IsNullOrEmpty(openAiKey))
                _openAiApiKey = openAiKey;

            var anthropicKey = _secureSettings.GetApiKey("anthropic");
            if (!string.IsNullOrEmpty(anthropicKey))
                _anthropicApiKey = anthropicKey;

            var settings    = _appSettingsService.Load();
            _maxAssumptions = settings.MaxAssumptions;
            _model          = settings.Model;
        }

        // == save all settings and forward keys to Engine for hot-reload == //
        private async Task SaveAsync()
        {
            try
            {
                // persist API keys via DPAPI and forward to Engine
                if (!string.IsNullOrWhiteSpace(OpenAiApiKey))
                {
                    _secureSettings.SaveApiKey("openai", OpenAiApiKey);
                    await ForwardKeyToEngineAsync("openai", OpenAiApiKey);
                }

                if (!string.IsNullOrWhiteSpace(AnthropicApiKey))
                {
                    _secureSettings.SaveApiKey("anthropic", AnthropicApiKey);
                    await ForwardKeyToEngineAsync("anthropic", AnthropicApiKey);
                }

                // persist non-secret settings
                _appSettingsService.Save(new AppSettings
                {
                    MaxAssumptions = MaxAssumptions,
                    Model          = Model
                });

                StatusMessage = "✓ Settings saved successfully!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"⚠ Error saving settings: {ex.Message}";
            }
        }

        // == forward API key to Engine so it can hot-reload without restart == //
        private async Task ForwardKeyToEngineAsync(string provider, string apiKey)
        {
            try
            {
                using var client  = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var body    = $"{{\"provider\":\"{provider}\",\"apiKey\":\"{EscapeJson(apiKey)}\"}}";
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                await client.PostAsync($"{_engineBaseUrl}/settings/apikey", content);
            }
            catch
            {
                // Engine may not be running yet; DPAPI save is the primary store
            }
        }

        // == minimal JSON string escape == //
        private static string EscapeJson(string s) =>
            s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}