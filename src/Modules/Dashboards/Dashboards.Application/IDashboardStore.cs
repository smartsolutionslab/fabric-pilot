using FabricPilot.Modules.Dashboards.Domain;

namespace FabricPilot.Modules.Dashboards.Application;

/// <summary>The dashboards table, one owner's rows at a time; implemented in the module's Infrastructure project.</summary>
public interface IDashboardStore
{
    /// <summary>The owner's dashboard, tracked so that <see cref="SaveAsync"/> saves what a command changes.</summary>
    Task<Option<Dashboard>> FindAsync(Guid id, string owner, CancellationToken cancellationToken);

    /// <summary>The owner's dashboard as it is stored, for a query: nothing that changes it is saved.</summary>
    Task<Option<Dashboard>> ReadAsync(Guid id, string owner, CancellationToken cancellationToken);

    /// <summary>One page of the owner's dashboards, by name, for a query.</summary>
    Task<PagedResult<Dashboard>> ListAsync(string owner, Page page, PageSize pageSize, CancellationToken cancellationToken);

    /// <summary>Every one of the owner's dashboards, by name, for a query that sums up all of them.</summary>
    Task<IReadOnlyList<Dashboard>> AllAsync(string owner, CancellationToken cancellationToken);

    void Add(Dashboard dashboard);

    void Remove(Dashboard dashboard);

    Task SaveAsync(CancellationToken cancellationToken);
}
