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

        public string EngineUrl
        {
            get => _engineUrl;
            set => SetProperty(ref _engineUrl, value);
        }

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
                    MaxAssumptions = MaxAssumptions
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