///// handles the data and state for the VS extension's UI /////

// == namespaces == //
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Extensibility.UI;
using AssumptionChecker.Contracts;
using AssumptionChecker.Core;

namespace AssumptionChecker.VsExtension
{
    [DataContract]
    internal class AssumptionCheckerData : INotifyPropertyChanged
    {
        // == private variables == //
        private readonly IAssumptionCheckerService _service;
        private string _promptText = string.Empty;
        private string _resultText = string.Empty;
        private string _isAnalyzing = "Collapsed";

        // == constructor == //
        public AssumptionCheckerData(IAssumptionCheckerService service)
        {
            _service = service;
            AnalyzeCommand = new AsyncCommand(async (parameter, cancellationToken) =>
            {
                await AnalyzeAsync(cancellationToken);
            });
        }

        [DataMember]
        public string PromptText // holds the user input prompt
        {
            get => _promptText;
            set
            {
                if (_promptText != value)
                {
                    _promptText = value;
                    OnPropertyChanged();
                }
            }
        }

        [DataMember]
        public string ResultText // holds the analysis results to display
        {
            get => _resultText;
            set
            {
                if (_resultText != value)
                {
                    _resultText = value;
                    OnPropertyChanged();
                }
            }
        }

        [DataMember]
        public string IsAnalyzing // controls visibility of "Analyzing..." text
        {
            get => _isAnalyzing;
            set
            {
                if (_isAnalyzing != value)
                {
                    _isAnalyzing = value;
                    OnPropertyChanged();
                }
            }
        }

        [DataMember]
        public IAsyncCommand AnalyzeCommand { get; } // analyzes the prompt when the button is clicked

        // == driver method for analyzing the prompt using the AssumptionChecker API == //
        private async Task AnalyzeAsync(CancellationToken cancellationToken)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(PromptText))
            {
                ResultText = "ERROR: Please enter a prompt to analyze.";
                return;
            }

            IsAnalyzing = "Visible";    // Show "Analyzing..." text
            ResultText  = string.Empty; // Clear previous results

            // == call the service and handle results/errors == //
            try
            {
                var result = await _service.AnalyzeAsync(PromptText, maxAssumptions: 10, cancellationToken);
                ResultText = FormatResults(result);
            }
            catch (Exception ex)
            {
                ResultText = $"ERROR: {ex.Message}\n\nMake sure the Engine is running:\n  cd AssumptionChecker.Engine\n  dotnet run";
            }
            finally
            {
                IsAnalyzing = "Collapsed";
            }
        }

        // == helper method to format the analysis results into a readable string == //
        private static string FormatResults(AnalyzeResponse response)
        {
            var output = new System.Text.StringBuilder();
            output.AppendLine($"Found {response.Assumptions.Count} assumption(s)");
            output.AppendLine($"Model: {response.Metadata.ModelUsed}, Latency: {response.Metadata.LatencyMs}ms\n");

            foreach (var assumption in response.Assumptions)
            {
                output.AppendLine($"[{assumption.RiskLevel}] {assumption.AssumptionText}");
                output.AppendLine($"  Category: {assumption.Category}");
                output.AppendLine($"  Rationale: {assumption.Rationale}");
                if (!string.IsNullOrWhiteSpace(assumption.ClarifyingQuestion))
                    output.AppendLine($"  Ask: {assumption.ClarifyingQuestion}");
                output.AppendLine();
            }

            if (response.SuggestedPrompts.Count > 0)
            {
                output.AppendLine("SUGGESTED IMPROVED PROMPTS:");
                for (int i = 0; i < response.SuggestedPrompts.Count; i++)
                {
                    output.AppendLine($"{i + 1}. {response.SuggestedPrompts[i]}");
                }
            }

            return output.ToString();
        }

        // ==  required for INotifyPropertyChanged implementation == //
        public event PropertyChangedEventHandler? PropertyChanged;

        // == helper method to raise PropertyChanged events == //
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
