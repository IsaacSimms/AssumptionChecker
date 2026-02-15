///// handles the data and state for the VS extension's UI /////

// == namespaces == //
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Extensibility;
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
        private readonly VisualStudioExtensibility _extensibility;
        private string _promptText  = string.Empty;
        private string _resultText  = string.Empty;
        private string _isAnalyzing = "Collapsed";

        // == constructor == //
        public AssumptionCheckerData(IAssumptionCheckerService service, VisualStudioExtensibility extensibility)
        {
            _service = service;
            _extensibility = extensibility;
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

        // == gathers content from all open documents in VS == //
        private async Task<List<FileContext>> GatherOpenFileContextsAsync(CancellationToken cancellationToken)
        {
            var fileContexts = new List<FileContext>();

            var documents = _extensibility.Documents();

            var openDocs  = await documents.GetOpenDocumentsAsync(cancellationToken);

            foreach (var doc in openDocs)
            {
                try
                {
                    // Get the document's file path
                    var filePath = doc.Moniker.ToString();

                    // Read the file content from disk
                    var content = await File.ReadAllTextAsync(filePath, cancellationToken);

                    // Skip very large files to keep the API payload reasonable
                    if (content.Length > 10_000)
                        content = content[..10_000] + "\n// ... (truncated)";

                    fileContexts.Add(new FileContext
                    {
                        FilePath = filePath,
                        Content = content
                    });
                }
                catch
                {
                    // Skip documents that can't be read (e.g., unsaved files, non-file documents)
                    continue;
                }
            }

            return fileContexts;
        }

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
                // Gather context from open files
                var fileContexts = await GatherOpenFileContextsAsync(cancellationToken);

                var result = await _service.AnalyzeAsync(PromptText, maxAssumptions: 10, fileContexts, cancellationToken);
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
