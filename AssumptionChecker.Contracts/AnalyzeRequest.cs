////// Defines the parameters for an analysis request to the assumption checker service. /////

// == namespaces == //
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssumptionChecker.Contracts
{
    public class AnalyzeRequest
    {
        // == Properties == //
        public required string Prompt { get; set; }              // The natural language description of the code or behavior to analyze. provvided by the user.
        public string Template        { get; set; } = "default"; // The name of the prompt template to use for analysis. This allows users to specify different templates for different types of analysis.
        public int MaxAssumptions     { get; set; } = 10;        // The maximum number of assumptions to return in the analysis response. Defaults to 10.
    }
}
