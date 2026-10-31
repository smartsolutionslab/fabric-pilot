using FabricPilot.BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FabricPilot.Modules.Billing.Infrastructure.Persistence;

/// <summary>Used by <c>dotnet ef</c> only: builds the context without the application or a database.</summary>
public sealed class BillingDbContextFactory : IDesignTimeDbContextFactory<BillingDbContext>
{
    public BillingDbContext CreateDbContext(string[] args)
    {
        const string connectionString = "Server=localhost";
        const string schema = BillingDbContext.Schema;
        var options = new DbContextOptionsBuilder<BillingDbContext>();
            options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", schema));
        return new BillingDbContext(options.Options, NoServices.Instance);
    }
}
