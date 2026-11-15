using FabricPilot.BuildingBlocks.Tenancy;
using FabricPilot.Modules.Notifications.Application;
using FabricPilot.Modules.Notifications.Application.DTOs;
using FabricPilot.Modules.Notifications.Domain.Notices;
using Microsoft.AspNetCore.Http;

namespace FabricPilot.Modules.Notifications.Tests;

/// <summary>
/// The notices of every tenant, in memory, each with the tenant that was current when it was saved, as the module's
/// DbContext stamps it; reads see the current tenant's rows only. What is added counts as stored only once saved.
/// </summary>
internal sealed class InMemoryNoticeStore : INoticeStore
{
    private readonly HttpTenantContext tenants = new(new HttpContextAccessor());

    private readonly List<Notice> pending = [];

    public List<(string? Tenant, Notice Notice)> Saved { get; } = [];

    public int Saves { get; private set; }

    public void Add(Notice notice) => pending.Add(notice);

    public Task<Option<Notice>> FindCausedByAsync(Guid eventIdentifier, CancellationToken cancellationToken)
    {
        var notice = Visible().SingleOrDefault(candidate => candidate.CausingEvent == eventIdentifier);
        return Task.FromResult(notice is null ? Option<Notice>.None : Option<Notice>.Some(notice));
    }

    public Task<PagedResult<NoticeDto>> ListAsync(string recipient, Page page, PageSize pageSize, CancellationToken cancellationToken)
    {
        Ensure.That(page).IsNotNull();
        Ensure.That(pageSize).IsNotNull();

        var mine = Visible().Where(notice => notice.Recipient == recipient).ToList();
        var items = mine.AsQueryable()
            .OrderByDescending(notice => notice.CreatedAt)
            .Skip(page.Skip(pageSize))
            .Take(pageSize.Value)
            .Select(NoticeMappings.ToNoticeDtoExpression());
        return Task.FromResult(new PagedResult<NoticeDto>([.. items], mine.Count, page.Value, pageSize.Value));
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        Saves++;
        Saved.AddRange(pending.Select(notice => (tenants.TenantId, notice)));
        pending.Clear();
        return Task.CompletedTask;
    }

    private IEnumerable<Notice> Visible() =>
        Saved.Where(row => row.Tenant == tenants.TenantId).Select(row => row.Notice);
}
