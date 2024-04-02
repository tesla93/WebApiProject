using Core;
using DataProcessing.Classes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace DataProcessing
{
    public class SignalRModuleLinkage : ISignalRModuleLinkage
    {
        void ISignalRModuleLinkage.MapHubs(IEndpointRouteBuilder routes) => 
            routes.MapHub<DataImportHub>("/api/import-processing");
    }
}
