using FabricPilot.BuildingBlocks.Api;
using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Dashboards.Application.Queries.DescribeWidget;
using FabricPilot.Modules.Dashboards.Application.Queries.ListWidgets;
using FabricPilot.Modules.Dashboards.Application.Queries.RunWidget;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FabricPilot.Modules.Dashboards.Api.Endpoints;

/// <summary>The widgets the modules declare that the caller may see, one of them by its id, and running one of them as the caller.</summary>
public sealed class WidgetEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        Ensure.That(endpoints).IsNotNull();

        endpoints.MapGet("/widgets", async (Page page, PageSize pageSize, IDispatcher dispatcher, CancellationToken cancellationToken) =>
            (await dispatcher.QueryAsync(new ListWidgets(page, pageSize), cancellationToken)).ToHttpResult());
        endpoints.MapGet("/widget-declarations/{id}", async (string id, IDispatcher dispatcher, CancellationToken cancellationToken) =>
            (await dispatcher.QueryAsync(new DescribeWidget(id), cancellationToken)).ToHttpResult());
        endpoints.MapGet("/widgets/{id}", async (string id, Page page, PageSize pageSize, IDispatcher dispatcher, CancellationToken cancellationToken) =>
            (await dispatcher.QueryAsync(new RunWidget(id, page, pageSize), cancellationToken)).ToHttpResult());
    }
}
