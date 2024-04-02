using Microsoft.AspNetCore.Routing;

namespace Core
{
    public interface ISignalRModuleLinkage
    {
        void MapHubs(IEndpointRouteBuilder routes);
    }
}
