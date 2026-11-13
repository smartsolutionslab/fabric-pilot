using FabricPilot.Modules.Notifications.Application.DTOs;
using FabricPilot.Modules.Notifications.Domain.Notices;

namespace FabricPilot.Modules.Notifications.Application;

/// <summary>The notices table, the current tenant's rows only; implemented in the module's Infrastructure project.</summary>
public interface INoticeStore
{
    void Add(Notice notice);

    /// <summary>The notice an integration event caused, tracked: what changes on it is saved.</summary>
    Task<Option<Notice>> FindCausedByAsync(Guid eventIdentifier, CancellationToken cancellationToken);

    /// <summary>One page of the recipient's notices, newest first, projected in the database.</summary>
    Task<PagedResult<NoticeDto>> ListAsync(string recipient, Page page, PageSize pageSize, CancellationToken cancellationToken);

    Task SaveAsync(CancellationToken cancellationToken);
}
