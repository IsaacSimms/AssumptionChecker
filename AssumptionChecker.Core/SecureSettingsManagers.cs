///// used for encrypted, secure storage. important for storing sensitive API keys /////

using System.Security.Cryptography;
using System.Text;

namespace AssumptionChecker.Core
{
    public static class SecureSettingsManagers
    {
        // == file path for storing the encrypted settings == //
        private static readonly string SettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AssumptionChecker", "settings.dat");

        // == saves a setting securely by encrypting the value and writing it to a file == //
        public static void SaveApiKey(string apiKey)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!); // ensure the directory exists

            // encrypt the API key using DPAPI with user scope
            var plainBytes = Encoding.UTF8.GetBytes(apiKey);
            var encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
        }
    }
}
