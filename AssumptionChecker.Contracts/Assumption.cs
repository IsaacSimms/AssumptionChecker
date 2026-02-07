///// defines the parameters of the assumption object /////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssumptionChecker.Contracts
{
    public class Assumption
    {
        // == Properties == //
        public required string Id                   { get; set; } // A unique identifier for the assumption, which can be used for tracking and reference purposes
        public required string AssumptionText       { get; set; } // The natural language description of the assumption identified in the analysis
        public required AssumptionCategory Category { get; set; } // The category of the assumption, which can be used to group similar assumptions together
        public required RiskLevel RiskLevel         { get; set; } // The level of risk associated with the assumption
        public required string ClarifyingQuestion   { get; set; } // A clarifying question that can be asked to validate or refute the assumption (make sure this is not forced on the user if not required for the assumption)
        public required string Rationale            { get; set; } // The rationale behind the assumption, explaining why it is considered valid or important
        public double ConfidenceScore               { get; set; } // A score representing the confidence in the assumption being true, typically ranging from 0 to 1
    }
}
