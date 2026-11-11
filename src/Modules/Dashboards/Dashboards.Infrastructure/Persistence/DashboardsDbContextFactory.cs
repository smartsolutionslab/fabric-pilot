using FabricPilot.BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FabricPilot.Modules.Dashboards.Infrastructure.Persistence;

/// <summary>Used by <c>dotnet ef</c> only: builds the context without the application or a database.</summary>
public sealed class DashboardsDbContextFactory : IDesignTimeDbContextFactory<DashboardsDbContext>
{
    public DashboardsDbContext CreateDbContext(string[] args)
    {
        const string connectionString = "Server=localhost";
        const string schema = DashboardsDbContext.Schema;
        var options = new DbContextOptionsBuilder<DashboardsDbContext>();
        options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", schema));
        return new DashboardsDbContext(options.Options, NoServices.Instance);
    }
}
