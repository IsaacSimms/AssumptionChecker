using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace AssumptionChecker.Core
{
    [SupportedOSPlatform("windows")]
    public class WindowsSecureSettingsManager : ISecureSettingsManager
    {
        // == settings file path == //
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "AssumptionChecker", 
            "settings.dat");

        // == save API key to settings file == //
        public void SaveApiKey(string apiKey)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            var plainBytes = Encoding.UTF8.GetBytes(apiKey);
            var encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(SettingsPath, encryptedBytes);
        }

        // == get API key, returns null if not found or on error == //
        public string? GetApiKey()
        {
            if (!File.Exists(SettingsPath)) return null; // No settings file means no API key saved

            try
            {
                var encryptedBytes = File.ReadAllBytes(SettingsPath);
                var plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                return null;
            }
        }
    }
}