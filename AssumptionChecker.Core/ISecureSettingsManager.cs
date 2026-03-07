namespace AssumptionChecker.Core
{
    public interface ISecureSettingsManager
    {
        // == legacy single-key methods (OpenAI default) == //
        void SaveApiKey(string apiKey);
        string? GetApiKey();

        // == named key methods for multi-provider support == //
        void SaveApiKey(string provider, string apiKey);
        string? GetApiKey(string provider);
    }
}