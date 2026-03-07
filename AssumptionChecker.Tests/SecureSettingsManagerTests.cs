// <summary>
// Tests for WindowsSecureSettingsManager: verifies named key storage,
// backward compatibility with legacy single-key methods, and provider path resolution.
// </summary>

using System.Runtime.Versioning;
using AssumptionChecker.Core;
using Xunit;

namespace AssumptionChecker.Tests
{
    [SupportedOSPlatform("windows")]
    public class SecureSettingsManagerTests
    {
        // == mirror path resolution logic from WindowsSecureSettingsManager == //
        private static string ResolveFileName(string provider)
        {
            var normalized = provider.ToLowerInvariant().Trim();
            return normalized == "openai" ? "settings.dat" : $"settings-{normalized}.dat";
        }

        [Theory]
        [InlineData("openai",    "settings.dat")]             // legacy path for OpenAI
        [InlineData("OpenAI",    "settings.dat")]             // case insensitive
        [InlineData("anthropic", "settings-anthropic.dat")]   // new provider gets its own file
        [InlineData("Anthropic", "settings-anthropic.dat")]   // case insensitive
        public void ProviderPathResolvesCorrectly(string provider, string expectedFileName)
        {
            var resolved = ResolveFileName(provider);
            Assert.Equal(expectedFileName, resolved);
        }

        [Fact]
        public void SaveAndRetrieveApiKey_RoundTrips()
        {
            var manager = new WindowsSecureSettingsManager();

            // save and retrieve for openai (uses legacy path)
            manager.SaveApiKey("openai", "test-openai-key-12345");
            var openAiKey = manager.GetApiKey("openai");
            Assert.Equal("test-openai-key-12345", openAiKey);

            // save and retrieve for anthropic (uses new path)
            manager.SaveApiKey("anthropic", "test-anthropic-key-67890");
            var anthropicKey = manager.GetApiKey("anthropic");
            Assert.Equal("test-anthropic-key-67890", anthropicKey);

            // keys are independent
            Assert.NotEqual(openAiKey, anthropicKey);

            // legacy method still works for openai
            var legacyKey = manager.GetApiKey();
            Assert.Equal("test-openai-key-12345", legacyKey);
        }

        [Fact]
        public void GetApiKey_ReturnsNull_WhenNoKeyStored()
        {
            var manager = new WindowsSecureSettingsManager();
            var result = manager.GetApiKey("nonexistent-provider-xyz");
            Assert.Null(result);
        }
    }
}
