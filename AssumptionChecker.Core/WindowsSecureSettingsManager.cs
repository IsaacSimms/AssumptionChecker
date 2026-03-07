// <summary>
// DPAPI-encrypted storage for API keys.
// Each provider gets its own .dat file under %APPDATA%\AssumptionChecker\.
// Legacy settings.dat maps to the "openai" provider for backward compat.
// </summary>

using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace AssumptionChecker.Core
{
#if !NETSTANDARD2_0
    [SupportedOSPlatform("windows")]
#endif
    public class WindowsSecureSettingsManager : ISecureSettingsManager
    {
        // == base directory for all settings files == //
        private static readonly string SettingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AssumptionChecker");

        // == legacy path (backward compat with existing OpenAI key) == //
        private static readonly string LegacySettingsPath = Path.Combine(SettingsDir, "settings.dat");

        // == resolve file path for a given provider == //
        private static string GetProviderPath(string provider)
        {
            var normalized = provider.ToLowerInvariant().Trim();
            if (normalized == "openai") return LegacySettingsPath; // keep existing file for OpenAI
            return Path.Combine(SettingsDir, $"settings-{normalized}.dat");
        }

        // == legacy: save OpenAI API key == //
        public void SaveApiKey(string apiKey) => SaveApiKey("openai", apiKey);

        // == legacy: get OpenAI API key == //
        public string? GetApiKey() => GetApiKey("openai");

        // == save API key for a specific provider == //
        public void SaveApiKey(string provider, string apiKey)
        {
            Directory.CreateDirectory(SettingsDir);
            var plainBytes     = Encoding.UTF8.GetBytes(apiKey);
            var encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(GetProviderPath(provider), encryptedBytes);
        }

        // == get API key for a specific provider, returns null if not found or on error == //
        public string? GetApiKey(string provider)
        {
            var path = GetProviderPath(provider);
            if (!File.Exists(path)) return null;

            try
            {
                var encryptedBytes = File.ReadAllBytes(path);
                var plainBytes     = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                return null;
            }
        }
    }
}
