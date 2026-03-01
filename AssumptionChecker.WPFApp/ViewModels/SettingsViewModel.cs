///// settings view model: manages API key, engine URL, and preferences /////

// == namespaces == //
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
        private readonly AppSettingsService _appSettingsService;
        private string _apiKey         = string.Empty;
        private string _engineUrl      = "http://localhost:5046";
        private int    _maxAssumptions = 10;
        private string _statusMessage  = string.Empty;

        // == constructor == //
        public SettingsViewModel(ISecureSettingsManager secureSettings, AppSettingsService appSettingsService)
        {
            _secureSettings     = secureSettings;
            _appSettingsService = appSettingsService;

            SaveCommand = new RelayCommand(_ => Save());

            LoadSettings();
        }

        // == bindable properties == //
        public string ApiKey
        {
            get => _apiKey;
            set
            {
                if (SetProperty(ref _apiKey, value))
                    OnPropertyChanged(nameof(MaskedApiKey));
            }
        }

        // == masked display: bullets for all but the last four characters == //
        public string MaskedApiKey => _apiKey.Length > 4
            ? new string('•', _apiKey.Length - 4) + _apiKey[^4..]
            : new string('•', _apiKey.Length);

        // == engine URL with basic validation == //
        public string EngineUrl
        {
            get => _engineUrl;
            set => SetProperty(ref _engineUrl, value);
        }

        // == available options for the OpenAI model == //
        public List<string> AvailableModels { get; } =
            [
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
        private string _openAiModel = "gpt-4o-mini"; // default model

        // take whatever model the user has selected and apply it to the private field, with a default fallback
        public string OpenAiModel
        {
            get => _openAiModel;
            set => SetProperty(ref _openAiModel, value);
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
            var savedKey = _secureSettings.GetApiKey();
            if (!string.IsNullOrEmpty(savedKey))
                _apiKey = savedKey;

            var settings    = _appSettingsService.Load();
            _engineUrl      = settings.EngineUrl;
            _maxAssumptions = settings.MaxAssumptions;
            _openAiModel    = settings.OpenAiModel;
        }

        // == save all settings == //
        private void Save()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(ApiKey))
                    _secureSettings.SaveApiKey(ApiKey);

                _appSettingsService.Save(new AppSettings
                {
                    EngineUrl      = EngineUrl,
                    MaxAssumptions = MaxAssumptions,
                    OpenAiModel    = OpenAiModel
                });

                StatusMessage = "✓ Settings saved successfully!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"⚠ Error saving settings: {ex.Message}";
            }
        }
    }
}