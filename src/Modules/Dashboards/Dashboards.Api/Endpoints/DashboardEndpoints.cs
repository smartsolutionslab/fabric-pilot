using FabricPilot.BuildingBlocks.Api;
using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Dashboards.Application.Commands.RemoveDashboard;
using FabricPilot.Modules.Dashboards.Application.Commands.SaveDashboard;
using FabricPilot.Modules.Dashboards.Application.Queries.GetDashboard;
using FabricPilot.Modules.Dashboards.Application.Queries.ListDashboards;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FabricPilot.Modules.Dashboards.Api.Endpoints;

/// <summary>
/// The caller's own dashboards. Saving is a PUT to an id the client chose and removing a DELETE, so every write can be
/// retried without applying twice.
/// </summary>
public sealed class DashboardEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        Ensure.That(endpoints).IsNotNull();

        endpoints.MapGet("/", async (Page page, PageSize pageSize, IDispatcher dispatcher, CancellationToken cancellationToken) =>
            (await dispatcher.QueryAsync(new ListDashboards(page, pageSize), cancellationToken)).ToHttpResult());
        endpoints.MapGet("/{id:guid}", async (Guid id, IDispatcher dispatcher, CancellationToken cancellationToken) =>
            (await dispatcher.QueryAsync(new GetDashboard(id), cancellationToken)).ToHttpResult());
        endpoints.MapPut("/{id:guid}", async (Guid id, SaveDashboardRequest request, IDispatcher dispatcher, CancellationToken cancellationToken) =>
            (await dispatcher.SendAsync(new SaveDashboard(id, request.Name, request.Widgets ?? []), cancellationToken)).ToHttpResult());
        endpoints.MapDelete("/{id:guid}", async (Guid id, IDispatcher dispatcher, CancellationToken cancellationToken) =>
            (await dispatcher.SendAsync(new RemoveDashboard(id), cancellationToken)).ToHttpResult());
    }
}
