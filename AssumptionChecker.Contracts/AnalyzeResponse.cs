///// defines the metadata parameters of the response /////

// == namespaces == //
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssumptionChecker.Contracts
{
    public class AnalyzeResponse
    {
        // == Properties == //
        public required List<Assumption> Assumptions { get; set; } // The list of assumptions identified in the analysis, along with their details.
        public required ResponseMetadata Metadata    { get; set; } // Metadata about the analysis response, such as processing time, model used, etc.
    }
}
