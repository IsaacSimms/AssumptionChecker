///// handles the data and state for the tool window UI /////

// == namespaces == //
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AssumptionChecker.Contracts;
using AssumptionChecker.Core;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using EnvDTE80;

namespace AssumptionChecker.VsExtension
{
    public class AssumptionCheckerViewModel : INotifyPropertyChanged
    {
        // == private fields == //
        private readonly IAssumptionCheckerService _service;
        private string _promptText  = string.Empty;
        private string _resultText  = string.Empty;
        private Visibility _isAnalyzing = Visibility.Collapsed;
        private bool _canAnalyze = true;

        // == constructor == //
        public AssumptionCheckerViewModel(IAssumptionCheckerService service)
        {
            _service        = service;
            AnalyzeCommand  = new RelayCommand(async _ => await AnalyzeAsync(), _ => _canAnalyze);
        }

        // == bindable properties == //
        public string PromptText
        {
            get => _promptText;
            set { _promptText = value; OnPropertyChanged(); }
        }

        public string ResultText
        {
            get => _resultText;
            set { _resultText = value; OnPropertyChanged(); }
        }

        public Visibility IsAnalyzing
        {
            get => _isAnalyzing;
            set { _isAnalyzing = value; OnPropertyChanged(); }
        }

        public ICommand AnalyzeCommand { get; }

        // == gathers context from all open documents in VS == //
        private List<FileContext> GatherOpenFileContexts()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var fileContexts = new List<FileContext>();

            if (!(Package.GetGlobalService(typeof(DTE)) is DTE2 dte))
                return fileContexts;

            foreach (Document doc in dte.Documents)
            {
                try
                {
                    var filePath = doc.FullName;
                    if (!File.Exists(filePath)) continue;

                    var content = File.ReadAllText(filePath);

                    if (content.Length > 10_000)
                        content = content.Substring(0, 10_000) + "\n// ... (truncated)";

                    fileContexts.Add(new FileContext
                    {
                        FilePath = filePath,
                        Content  = content
                    });
                }
                catch { continue; }
            }

            return fileContexts;
        }

        // == driver method for analyzing the prompt == //
        private async Task AnalyzeAsync()
        {
            if (string.IsNullOrWhiteSpace(PromptText))
            {
                ResultText = "ERROR: Please enter a prompt to analyze.";
                return;
            }

            _canAnalyze = false;
            IsAnalyzing = Visibility.Visible;
            ResultText  = string.Empty;

            try
            {
                // Gather file contexts on UI thread (DTE requires it)
                List<FileContext> fileContexts;
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                fileContexts = GatherOpenFileContexts();

                // Call the engine on a background thread
                var result = await Task.Run(() =>
                        _service.AnalyzeAsync(PromptText, maxAssumptions: 10, fileContexts: fileContexts, cancellationToken: CancellationToken.None));

                ResultText = FormatResults(result);
            }
            catch (Exception ex)
            {
                ResultText = $"ERROR: {ex.Message}\n\nMake sure the Engine is running:\n  cd AssumptionChecker.Engine\n  dotnet run";
            }
            finally
            {
                IsAnalyzing = Visibility.Collapsed;
                _canAnalyze = true;
            }
        }

        // == format results (unchanged logic) == //
        private static string FormatResults(AnalyzeResponse response)
        {
            var output = new StringBuilder();
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
                    output.AppendLine($"{i + 1}. {response.SuggestedPrompts[i]}");
            }

            return output.ToString();
        }

        // == INotifyPropertyChanged == //
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}