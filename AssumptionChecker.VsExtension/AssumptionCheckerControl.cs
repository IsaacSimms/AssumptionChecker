// == namespaces == //
using Microsoft.VisualStudio.Extensibility.UI;
using System.Runtime.Serialization;

namespace AssumptionChecker.VsExtension
{
    [DataContract]
    internal class AssumptionCheckerControl : RemoteUserControl
    {
        public AssumptionCheckerControl(AssumptionCheckerData dataContext) 
            : base(dataContext) 
        { 
        }
    }
}
