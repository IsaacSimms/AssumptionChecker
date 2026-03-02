///// 

using AssumptionChecker.WPFApp.Models;
using AssumptionChecker.WPFApp.Services;
using System;
using System.IO;
using Xunit;


namespace AssumptionChecker.Tests
{
    public class AppSettingsServiceTests : IDisposable
    {
        // == private variables == //
        private readonly string _tempDir;
        private readonly string _tempFile;

        // == constructor & dispose == //
        public AppSettingsServiceTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"ac_test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
            _tempFile = Path.Combine(_tempDir, "appsettings.json");
        }
        public void Dispose() => Directory.Delete(_tempDir, recursive: true);

        [Fact]
        public void SaveAndLoadPreserveOpenAiModel()
        {
            // Arrange
            var json = """
                {
                    "EngineUrl": "http://localhost:5046",
                    "MaxAssumptions": 10,
                    "OpenAiModel": "gpt-4.1"
                }
                """;
            File.WriteAllText(_tempFile, json);
            
            // act
            var loaded = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_tempFile)); 

            // assert
            Assert.NotNull(loaded);
            Assert.Equal("gpt-4.1", loaded.OpenAiModel);
        }

        
        // == tests to ensure that 40mini is default when config is missing or untouched == //
        [Fact]
        public void MissingOpenAiModelDefaultsTogpt4omini()
        {
            // arrange
            var json = """
        {
          "EngineUrl": "http://localhost:5046",
          "MaxAssumptions": 10
        }
        """;
            File.WriteAllText(_tempFile, json);

            // act
            var loaded = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_tempFile));

            // assert
            Assert.NotNull(loaded);
            Assert.Equal("gpt-4o-mini", loaded.OpenAiModel);
        }

    }
}
