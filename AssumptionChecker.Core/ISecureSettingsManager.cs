namespace AssumptionChecker.Core
{
    public interface ISecureSettingsManager
    {
        void SaveApiKey(string apiKey);
        string? GetApiKey();
    }
}