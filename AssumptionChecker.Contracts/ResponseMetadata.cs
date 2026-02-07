///// defines the metatedata parameters of the response /////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssumptionChecker.Contracts
{
    // == metadata parameters of the response == //
    public class ResponseMetadata
    {
        public required string ModelUsed { get; set; } // The name or identifier of the model used to generate the analysis response
        public int TokensUsed            { get; set; } // The total number of tokens used in the analysis
        public long LatencyMs            { get; set; } // The latency of the analysis process in milliseconds
    }
}
