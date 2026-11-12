using FabricPilot.BuildingBlocks.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FabricPilot.Modules.Tickets.Api;

public sealed class TicketsModule : IModule
{
    public void AddServices(IServiceCollection services, IConfiguration configuration) =>
        services.AddModuleInfrastructure<TicketsModule>(configuration).AddTicketsStorage(configuration);

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGroup("/tickets").MapModuleEndpoints<TicketsModule>();
}
