using AssumptionChecker.Contracts;
using Xunit;

namespace AssumptionChecker.Tests
{
    public class AnalyzeRequestTests
    {
        [Fact]
        public void DefaultModelIsGpt4omini()
        {
            var request = new AnalyzeRequest { Prompt = "test prompt" };
            Assert.Equal("gpt-4o-mini", request.Model);
        }

        [Fact]
        public void ModelCanBeOverridden()
        {
            var request = new AnalyzeRequest { Prompt = "test prompt", Model = "gpt-4.1" };
            Assert.Equal("gpt-4.1", request.Model);
        }
    }
}
