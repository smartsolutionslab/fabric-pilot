using FabricPilot.Modules.Notifications.Application;
using FabricPilot.Modules.Notifications.Application.DTOs;
using FabricPilot.Modules.Notifications.Domain.Notices;
using FabricPilot.Modules.Notifications.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FabricPilot.Modules.Notifications.Infrastructure;

/// <summary>Every read is filtered to the current tenant by the DbContext's query filter.</summary>
public sealed class NoticeStore(NotificationsDbContext context) : INoticeStore
{
    public void Add(Notice notice) => context.Add(notice);

    public async Task<Option<Notice>> FindCausedByAsync(Guid eventIdentifier, CancellationToken cancellationToken)
    {
        var notice = await context.Set<Notice>().SingleOrDefaultAsync(candidate => candidate.CausingEvent == eventIdentifier, cancellationToken);
        return notice is null ? Option<Notice>.None : Option<Notice>.Some(notice);
    }

    public async Task<PagedResult<NoticeDto>> ListAsync(string recipient, Page page, PageSize pageSize, CancellationToken cancellationToken)
    {
        Ensure.That(recipient).IsNotNullOrWhiteSpace();
        Ensure.That(page).IsNotNull();
        Ensure.That(pageSize).IsNotNull();

        var notices = context.Set<Notice>().AsNoTracking().Where(notice => notice.Recipient == recipient);
        var total = await notices.CountAsync(cancellationToken);
        var items = await notices
            .OrderByDescending(notice => notice.CreatedAt)
            .ThenByDescending(notice => notice.Id)
            .Skip(page.Skip(pageSize))
            .Take(pageSize.Value)
            .Select(NoticeMappings.ToNoticeDtoExpression())
            .ToListAsync(cancellationToken);
        return new PagedResult<NoticeDto>(items, total, page.Value, pageSize.Value);
    }

    public Task SaveAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
