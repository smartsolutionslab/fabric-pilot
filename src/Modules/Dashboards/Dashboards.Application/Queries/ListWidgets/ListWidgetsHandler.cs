using FabricPilot.BuildingBlocks.Dashboards;
using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Dashboards.Application.DTOs;

namespace FabricPilot.Modules.Dashboards.Application.Queries.ListWidgets;

public sealed class ListWidgetsHandler(WidgetCatalog catalog, IWidgetAccess access)
    : IQueryHandler<ListWidgets, Result<PagedResult<WidgetDescriptionDto>, ApiError>>
{
    public async Task<Result<PagedResult<WidgetDescriptionDto>, ApiError>> HandleAsync(ListWidgets query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        List<WidgetDefinition> visible = [];
        foreach (var widget in catalog.Widgets)
        {
            if (await access.AllowsAsync(widget, cancellationToken))
            {
                visible.Add(widget);
            }
        }

        List<WidgetDescriptionDto> items = [.. visible.Skip(query.Page.Skip(query.PageSize)).Take(query.PageSize.Value).Select(widget => widget.ToDto())];
        return Success(new PagedResult<WidgetDescriptionDto>(items, visible.Count, query.Page.Value, query.PageSize.Value));
    }
}
