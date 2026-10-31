using Microsoft.AspNetCore.Routing;

namespace FabricPilot.BuildingBlocks.Api;

/// <summary>One endpoint or a few closely related ones. Found by assembly scan in the module's Api project.</summary>
public interface IEndpoint
{
    void Map(IEndpointRouteBuilder endpoints);
}
