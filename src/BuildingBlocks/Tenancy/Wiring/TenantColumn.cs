using System.Linq.Expressions;
using FabricPilot.BuildingBlocks.Persistence;

namespace Microsoft.EntityFrameworkCore;

public static class TenantColumn
{
    private const string Column = "TenantId";

    /// <summary>
    /// A TenantId shadow column on every entity and a filter to the context's current tenant, or to every tenant inside
    /// AcrossTenants(); an entity shared by all tenants (SharedByAllTenants()) has neither.
    /// </summary>
    public static ModelBuilder ApplyTenantColumn(this ModelBuilder modelBuilder, ModuleDbContext context)
    {
        Ensure.That(modelBuilder).IsNotNull();

        var currentTenant = Expression.Property(Expression.Constant(context), nameof(ModuleDbContext.CurrentTenantId));
        var allTenants = Expression.Property(Expression.Constant(context), nameof(ModuleDbContext.ReadsAllTenants));
        modelBuilder.Model.CheckSharedRecords();
        foreach (var entity in modelBuilder.Model.GetEntityTypes().Where(entity => !entity.IsOwned() && entity.BaseType is null && !entity.IsSharedByAllTenants()))
        {
            modelBuilder.Entity(entity.ClrType).Property<string>(Column).HasMaxLength(64).IsRequired();
            modelBuilder.Entity(entity.ClrType).HasIndex(Column);

            var row = Expression.Parameter(entity.ClrType, "row");
            var rowTenant = Expression.Call(typeof(EF), nameof(EF.Property), [typeof(string)], row, Expression.Constant(Column));
            modelBuilder.Entity(entity.ClrType).HasQueryFilter(Expression.Lambda(Expression.OrElse(allTenants, Expression.Equal(rowTenant, currentTenant)), row));
        }

        return modelBuilder;
    }

    /// <summary>
    /// Stamps new rows with the current tenant; saving one without a tenant fails, and so does saving anything inside
    /// AcrossTenants(). Records shared by all tenants are saved only inside WritesSharedRecords(), and nothing else is.
    /// </summary>
    public static void StampTenant(this ModuleDbContext context)
    {
        Ensure.That(context).IsNotNull();

        // Rows read across tenants belong to several of them, and a write needs exactly one.
        if (context.ReadsAllTenants && context.ChangeTracker.HasChanges())
        {
            throw new InvalidOperationException("Cannot save inside AcrossTenants(): reading every tenant's rows writes for none of them.");
        }

        // A shared record is every tenant's, so only staff write it; and staff writing them have no tenant to write for.
        foreach (var entry in context.ChangeTracker.Entries().Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var shared = entry.Metadata.IsSharedByAllTenants();
            if (shared && !context.WritesSharedRecords)
            {
                throw new InvalidOperationException(
                    $"Cannot save a {entry.Metadata.ClrType.Name} outside WritesSharedRecords(): records shared by all tenants are written only on an endpoint mapped with it.");
            }

            if (!shared && context.WritesSharedRecords)
            {
                throw new InvalidOperationException(
                    $"Cannot save a {entry.Metadata.ClrType.Name} inside WritesSharedRecords(): it writes the records shared by all tenants, for no tenant.");
            }
        }

        foreach (var entry in context.ChangeTracker.Entries().Where(entry => entry.State == EntityState.Added && entry.Metadata.FindProperty(Column) is not null))
        {
            entry.Property(Column).CurrentValue = context.CurrentTenantId
                ?? throw new InvalidOperationException($"Cannot save a new {entry.Metadata.ClrType.Name} without a tenant.");
        }
    }
}
