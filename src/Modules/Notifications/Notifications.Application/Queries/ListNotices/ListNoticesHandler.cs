using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Notifications.Application.DTOs;

namespace FabricPilot.Modules.Notifications.Application.Queries.ListNotices;

public sealed class ListNoticesHandler(INoticeStore notices) : IQueryHandler<ListNotices, Result<PagedResult<NoticeDto>, ApiError>>
{
    public async Task<Result<PagedResult<NoticeDto>, ApiError>> HandleAsync(ListNotices query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        var (recipient, page, pageSize) = query;
        return Success(await notices.ListAsync(recipient, page, pageSize, cancellationToken));
    }
}
