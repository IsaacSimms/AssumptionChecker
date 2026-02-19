///// shares a markdown/text formatting used in responses                 /////
///// // format the response in markdown for better readability in the UI /////
// == namespaces == //
using System.Text;
using AssumptionChecker.Contracts;

namespace AssumptionChecker.Core
{
    public static class ResponseFormatter
    {
        public static string FormatAnalyzeResponse(AnalyzeResponse response)
        {
            var sb = new StringBuilder(); // used for string concatenation

            // formats headers and metadata
            sb.AppendLine("// == Assumptions: == //");
            sb.AppendLine($"Found **{response.Assumptions.Count} assumption(s):" +
                          $"({response.Metadata.ModelUsed}, {response.Metadata.LatencyMs}ms)");

            // format each section of assumptions
            foreach (var a in response.Assumptions)
            {
                // risk levels
                var riskIcon = a.RiskLevel switch
                {
                    RiskLevel.Low => "🟢",
                    RiskLevel.Medium => "🟡",
                    RiskLevel.High => "🔴",
                    _ => ""
                };

                // assumption details
                sb.AppendLine($"### {riskIcon} [{a.RiskLevel.ToString().ToUpper()}] {a.AssumptionText}");
                sb.AppendLine($"- **Category:** {a.Category}");
                if (!string.IsNullOrWhiteSpace(a.ClarifyingQuestion))
                    sb.AppendLine($"- **Ask:** {a.ClarifyingQuestion}");
                sb.AppendLine($"- **Rationale:** {a.Rationale}");
                sb.AppendLine($"- **Confidence:** {a.Confidence:P0}");
                sb.AppendLine();
            }

            // clarifying questions section
            var questions = response.Assumptions
                .Where(a => !string.IsNullOrWhiteSpace(a.ClarifyingQuestion))
                .Select(a => $"- {a.ClarifyingQuestion}")
                .ToList();

            // handle no questions case
            if (questions.Count > 0)
            {
                sb.AppendLine("### ❓ Clarifying Questions");
                foreach (var q in questions)
                    sb.AppendLine(q);
                sb.AppendLine();
            }

            // suggested prompts section
            if (response.SuggestedPrompts.Count > 0)
            {
                sb.AppendLine("### ✨ Suggested Improved Prompts");
                sb.AppendLine("Click an option below to load it into the chat input:");
                sb.AppendLine();
                sb.AppendLine("- **\\[0\\]** Keep your original prompt");
                for (int i = 0; i < response.SuggestedPrompts.Count; i++)
                {
                    sb.AppendLine($"- **\\[{i + 1}\\]** {response.SuggestedPrompts[i]}");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}