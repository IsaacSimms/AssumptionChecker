
// == namespaces == //
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AssumptionChecker.Contracts;
using AssumptionChecker.Core;
using AssumptionChecker.WPFApp.Models;
using AssumptionChecker.WPFApp.Services;
using AssumptionChecker.WPFApp.ViewModels;
using Moq;
using Xunit;


namespace AssumptionChecker.Tests
{
    public class MainViewModelTests
    {
        [Theory]
        [InlineData("gpt-4.1")]
        [InlineData("o3-mini")]
        [InlineData("gpt-4o")]
        public async Task SendAsyncPassesSavedModelToService(string savedModel)
        {
            // arrange
            string? capturedModel = null;

            // fake settings returning chosen model. Does not use the real file == //
            var mockSettings = new Mock<IAppSettingsService>();
            mockSettings.Setup(s => s.Load()).Returns(new AppSettings { 
                EngineUrl = "http://localhost:5000", 
                OpenAiModel = savedModel,
                MaxAssumptions = 5
            });

            // capture the model passed to the service
            var mockService = new Mock<IAssumptionCheckerService>();
            mockService.Setup (s => s.AnalyzeAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<List<FileContext>>(),
                It.IsAny<CancellationToken>()))
                .Callback<string, int, string, List<FileContext>?, CancellationToken>((_, _, model, _, _) => capturedModel = model)
                .ReturnsAsync(new AnalyzeResponse 
                {
                    Assumptions = new List<Assumption>(),
                    SuggestedPrompts = new List<string>(),
                    Metadata = new ResponseMetadata { ModelUsed = savedModel, LatencyMs = 1, TokensUsed = 1 }
                });
            var settingsVm = new SettingsViewModel(
                new Mock<ISecureSettingsManager>().Object,
                mockSettings.Object);

            var vm = new MainViewModel(mockService.Object, mockSettings.Object, settingsVm);
        }
    }
}
