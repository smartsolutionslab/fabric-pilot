using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace FabricPilot.BuildingBlocks.Modules;

public static class ModuleEndpoints
{
    /// <summary>Maps one module's endpoints into the route group the module was given.</summary>
    public static RouteGroupBuilder MapModuleEndpoints<TModule>(this RouteGroupBuilder group)
        where TModule : IModule
    {
        Ensure.That(group).IsNotNull();
        group.RequireAuthorization();
        group.MapEndpointsIn(typeof(TModule).Assembly);
        return group;
    }
}
