using AssumptionChecker.Contracts;
using Xunit;

namespace AssumptionChecker.Tests
{
    public class AnalyzeRequestTests
    {
        [Fact]
        public void DefaultModelIsClaudeHaiku()
        {
            var request = new AnalyzeRequest { Prompt = "test prompt" };
            Assert.Equal("claude-haiku-4-5", request.Model);
        }

        [Fact]
        public void ModelCanBeOverridden()
        {
            var request = new AnalyzeRequest { Prompt = "test prompt", Model = "gpt-4.1" };
            Assert.Equal("gpt-4.1", request.Model);
        }
    }
}
