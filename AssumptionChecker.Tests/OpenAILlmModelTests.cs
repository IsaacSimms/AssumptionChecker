using System.Collections.Generic;
using Xunit;

namespace AssumptionChecker.Tests
{
    public class OpenAILlmModelTests
    {
        // == mirror fallback logic that occurs in OpenAILlmClient.AnalyzeAsync == //
        private static string ResolveModel(string? input)
            => string.IsNullOrWhiteSpace(input) ? "gpt-4o-mini" : input;

        [Theory]
        // == inline data tests for various input scenarios, including null, empty, whitespace, and valid model names == //
        [InlineData(null,      "gpt-4o-mini")]
        [InlineData("",        "gpt-4o-mini")]
        [InlineData("   ",     "gpt-4o-mini")]
        [InlineData("gpt-4.1", "gpt-4.1")]
        [InlineData("o3-mini", "o3-mini")] 
        [InlineData("gpt-4o",  "gpt-4o")]

        public void ModelFallbackResolvesCorrectly(string? input, string expected)
        {
            var resolved = ResolveModel(input);
            Assert.Equal(expected, resolved);
        }
    }
}
