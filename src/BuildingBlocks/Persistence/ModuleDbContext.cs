using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FabricPilot.BuildingBlocks.Persistence;

/// <summary>The base of every module's DbContext: the module's own schema and its entity configurations.</summary>
public abstract class ModuleDbContext(DbContextOptions options, IServiceProvider services, string schema) : DbContext(options)
{
    /// <summary>The application's services, for conventions that depend on the current request.</summary>
    public IServiceProvider Services { get; } = services;

    /// <summary>The tenant whose rows this context sees (tenancy: column).</summary>
    public string? CurrentTenantId => Services.GetService<FabricPilot.BuildingBlocks.Tenancy.ITenantContext>()?.TenantId;

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        BeforeSaving();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        BeforeSaving();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        Ensure.That(modelBuilder).IsNotNull();

        modelBuilder.HasDefaultSchema(schema);
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
        modelBuilder.ApplyTenantColumn(this);
        base.OnModelCreating(modelBuilder);
    }

    private void BeforeSaving()
    {
        this.StampTenant();
    }
}
