using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace FabricPilot.BuildingBlocks.Persistence;

/// <summary>The base of every module's DbContext: the module's own schema and its entity configurations.</summary>
public abstract class ModuleDbContext(DbContextOptions options, IServiceProvider services, string schema) : DbContext(options)
{
    /// <summary>The application's services, for conventions that depend on the current request.</summary>
    public IServiceProvider Services { get; } = services;

    /// <summary>The tenant whose rows this context sees (tenancy: column).</summary>
    public string? CurrentTenantId => Services.GetService<FabricPilot.BuildingBlocks.Tenancy.ITenantContext>()?.TenantId;

    /// <summary>Whether this context sees every tenant's rows: inside an endpoint mapped with AcrossTenants(). It then saves nothing.</summary>
    public bool ReadsAllTenants => Services.GetService<FabricPilot.BuildingBlocks.Tenancy.ITenantContext>()?.ReadsAllTenants ?? false;

    /// <summary>Whether this context writes the records shared by all tenants: inside an endpoint mapped with WritesSharedRecords(). It then saves nothing else.</summary>
    public bool WritesSharedRecords => Services.GetService<FabricPilot.BuildingBlocks.Tenancy.ITenantContext>()?.WritesSharedRecords ?? false;

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

    /// <summary>
    /// Applies the module's migrations. A module starts with none, and EF Core 11 refuses to migrate a context that has
    /// none (MigrationsNotFound). Without migrations the module gets its database and its migrations history table, as
    /// EF Core 10 gave it; with them, a model that changed since the last one still fails, in MigrateAsync.
    /// </summary>
    public async Task MigrateModuleAsync(CancellationToken cancellationToken = default)
    {
        if (Database.GetMigrations().Any())
        {
            await Database.MigrateAsync(cancellationToken);
            return;
        }

        var database = Database.GetService<IRelationalDatabaseCreator>();
        if (!await database.ExistsAsync(cancellationToken))
        {
            await database.CreateAsync(cancellationToken);
        }

        await Database.GetService<IHistoryRepository>().CreateIfNotExistsAsync(cancellationToken);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        Ensure.That(optionsBuilder).IsNotNull();

        // Per instance: an interceptor may keep state for the context it belongs to.
        optionsBuilder.AddInterceptors(
            new FabricPilot.BuildingBlocks.Tenancy.AllTenantsWriteNothing(this),
            new FabricPilot.BuildingBlocks.Tenancy.SharedRecordsBulkWriteGuard(this),
            new FabricPilot.BuildingBlocks.DomainEvents.DomainEventDispatch(Services));
        base.OnConfiguring(optionsBuilder);
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
