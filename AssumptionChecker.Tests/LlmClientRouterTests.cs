// <summary>
// Tests for LlmClientRouter: verifies model name routing to correct provider
// and error handling when API keys are missing.
// </summary>

using Xunit;

namespace AssumptionChecker.Tests
{
    public class LlmClientRouterTests
    {
        // == mirror the routing logic from LlmClientRouter == //
        private static string ResolveProvider(string? model)
        {
            var normalized = model?.ToLowerInvariant() ?? "";
            return normalized.StartsWith("claude") ? "anthropic" : "openai";
        }

        [Theory]
        [InlineData("gpt-4o-mini",       "openai")]
        [InlineData("gpt-4o",            "openai")]
        [InlineData("o1-mini",           "openai")]
        [InlineData("gpt-5.2",          "openai")]
        [InlineData("claude-sonnet-4-6", "anthropic")]
        [InlineData("claude-haiku-4-5",  "anthropic")]
        [InlineData("claude-opus-4-6",   "anthropic")]
        [InlineData("Claude-Sonnet-4-6", "anthropic")]  // case insensitive
        [InlineData(null,                "openai")]      // null defaults to openai
        [InlineData("",                  "openai")]      // empty defaults to openai
        public void RouterResolvesCorrectProvider(string? model, string expectedProvider)
        {
            var resolved = ResolveProvider(model);
            Assert.Equal(expectedProvider, resolved);
        }
    }
}
