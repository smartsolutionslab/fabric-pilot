using FabricPilot.Modules.Dashboards.Application;
using FabricPilot.Modules.Dashboards.Domain;
using FabricPilot.Modules.Dashboards.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FabricPilot.Modules.Dashboards.Infrastructure;

/// <summary>Every read filters by owner here, and by tenant in the DbContext's query filter: a user reads only their own dashboards.</summary>
public sealed class DashboardStore(DashboardsDbContext context) : IDashboardStore
{
    public async Task<Option<Dashboard>> FindAsync(Guid id, string owner, CancellationToken cancellationToken) =>
        Found(await context.Set<Dashboard>().SingleOrDefaultAsync(dashboard => dashboard.Id == id && dashboard.Owner == owner, cancellationToken));

    public async Task<Option<Dashboard>> ReadAsync(Guid id, string owner, CancellationToken cancellationToken) =>
        Found(await context.Set<Dashboard>().AsNoTracking().SingleOrDefaultAsync(dashboard => dashboard.Id == id && dashboard.Owner == owner, cancellationToken));

    public async Task<PagedResult<Dashboard>> ListAsync(string owner, Page page, PageSize pageSize, CancellationToken cancellationToken)
    {
        Ensure.That(page).IsNotNull();
        Ensure.That(pageSize).IsNotNull();

        var owned = context.Set<Dashboard>().AsNoTracking().Where(dashboard => dashboard.Owner == owner);
        var total = await owned.CountAsync(cancellationToken);
        var items = await owned
            .OrderBy(dashboard => dashboard.Name)
            .ThenBy(dashboard => dashboard.Id)
            .Skip(page.Skip(pageSize))
            .Take(pageSize.Value)
            .ToListAsync(cancellationToken);
        return new PagedResult<Dashboard>(items, total, page.Value, pageSize.Value);
    }

    public async Task<IReadOnlyList<Dashboard>> AllAsync(string owner, CancellationToken cancellationToken) =>
        await context.Set<Dashboard>().AsNoTracking()
            .Where(dashboard => dashboard.Owner == owner)
            .OrderBy(dashboard => dashboard.Name)
            .ThenBy(dashboard => dashboard.Id)
            .ToListAsync(cancellationToken);

    public void Add(Dashboard dashboard) => context.Add(dashboard);

    public void Remove(Dashboard dashboard) => context.Remove(dashboard);

    public Task SaveAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);

    private static Option<Dashboard> Found(Dashboard? dashboard) =>
        dashboard is null ? Option<Dashboard>.None : Option<Dashboard>.Some(dashboard);
}
