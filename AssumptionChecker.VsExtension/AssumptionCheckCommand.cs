///// Command to analyze a prompt via input dialog /////

// == namespaces == //
using System.Diagnostics;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using AssumptionChecker.Core;
using AssumptionChecker.Contracts;

namespace AssumptionChecker.VsExtension
{
    [VisualStudioContribution] // marks this class as a contribution to the VS extension

    // == handles the command to analyze prompts for assumptions == // 
    internal class AssumptionCheckCommand : Command
    {
        private readonly IAssumptionCheckerService _service;

        public AssumptionCheckCommand(IAssumptionCheckerService service)
        {
            _service = service;
        }

        public override CommandConfiguration CommandConfiguration => new("Analyze Prompt Assumptions")
        {
            Placements = new[] { CommandPlacement.KnownPlacements.ToolsMenu },
            Icon = new(ImageMoniker.KnownValues.StatusInformation, IconSettings.None),
        };

        // == main command execution logic == //
        public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
        {
            string? prompt = null;

            try
            {
                // Try to get selected text from the active document
                var textView = await context.GetActiveTextViewAsync(cancellationToken);
                if (textView?.Selection is not null)
                {
                    var selection = await textView.GetSelectionAsync(cancellationToken);
                    if (selection?.Text is not null && !string.IsNullOrWhiteSpace(selection.Text))
                    {
                        prompt = selection.Text;
                        Trace.WriteLine($"Using selected text as prompt ({prompt.Length} chars)");
                    }
                }
            }
            catch
            {
                // Selection API might not be available, continue to clipboard fallback
            }

            // Fallback: try clipboard if no selection
            if (string.IsNullOrWhiteSpace(prompt))
            {
                try
                {
                    if (System.Windows.Clipboard.ContainsText())
                    {
                        prompt = System.Windows.Clipboard.GetText();
                        Trace.WriteLine($"Using clipboard content as prompt ({prompt?.Length ?? 0} chars)");
                    }
                }
                catch
                {
                    // Clipboard access might fail
                }
            }

            // Validate we have a prompt
            if (string.IsNullOrWhiteSpace(prompt))
            {
                Trace.WriteLine("ERROR: No prompt provided. Either:");
                Trace.WriteLine("  1. Select text in the editor, then run Tools > Analyze Prompt Assumptions");
                Trace.WriteLine("  2. Copy your prompt to clipboard, then run Tools > Analyze Prompt Assumptions");
                return;
            }

            try
            {
                Trace.WriteLine("=== ASSUMPTION CHECKER ===");
                Trace.WriteLine($"Analyzing prompt: {prompt.Substring(0, Math.Min(100, prompt.Length))}...");
                
                // Call the assumption checker service
                var result = await _service.AnalyzeAsync(prompt, maxAssumptions: 10, cancellationToken);

                // Format and output results
                var output = FormatResults(result);
                Trace.WriteLine(output);
                Trace.WriteLine("=== ANALYSIS COMPLETE ===");
            }
            catch (HttpRequestException ex)
            {
                Trace.WriteLine($"ERROR: Could not reach Engine. {ex.Message}");
                Trace.WriteLine("Make sure the Engine is running:");
                Trace.WriteLine("  cd AssumptionChecker.Engine");
                Trace.WriteLine("  dotnet run");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"ERROR: {ex.Message}");
                Trace.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        // == helper to format the analysis results for output terminal == //
        private static string FormatResults(AnalyzeResponse response)
        {
            var output = new System.Text.StringBuilder();
            output.AppendLine($"\nFound {response.Assumptions.Count} assumption(s)");
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
    }
}
