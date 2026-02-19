////// Defines the parameters for an analysis request to the assumption checker service. /////

// == namespaces == //
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace AssumptionChecker.Contracts
{
    public class AnalyzeRequest
    {
        // == Properties == //
        public required string Prompt         { get; set; }                   // The natural language description of the code or behavior to analyze. provvided by the user.
        public string Template                { get; set; } = "default";      // The name of the prompt template to use for analysis. This allows users to specify different templates for different types of analysis.
        public int MaxAssumptions             { get; set; } = 10;             // The maximum number of assumptions to return in the analysis response. Defaults to 10.
        public List<FileContext> FileContexts { get; set; } = new();          // Optional list of files to provide additional context for the analysis. Each file includes its path and content. Used in VS extension
    }

    public class FileContext
    {
        public required string FilePath { get; set; } // The path of the file being analyzed.
        public required string Content  { get; set; } // content of file (code or otherwise)
    }
}
