using FabricPilot.BuildingBlocks.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FabricPilot.Modules.Assistant.Api;

public sealed class AssistantModule : IModule
{
    public void AddServices(IServiceCollection services, IConfiguration configuration) =>
        services.AddModuleInfrastructure<AssistantModule>(configuration);

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGroup("/assistant").MapModuleEndpoints<AssistantModule>();
}
