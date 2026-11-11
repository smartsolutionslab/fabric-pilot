using FabricPilot.BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FabricPilot.Modules.Contracts.Infrastructure.Persistence;

/// <summary>Used by <c>dotnet ef</c> only: builds the context without the application or a database.</summary>
public sealed class ContractsDbContextFactory : IDesignTimeDbContextFactory<ContractsDbContext>
{
    public ContractsDbContext CreateDbContext(string[] args)
    {
        const string connectionString = "Server=localhost";
        const string schema = ContractsDbContext.Schema;
        var options = new DbContextOptionsBuilder<ContractsDbContext>();
        options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", schema));
        return new ContractsDbContext(options.Options, NoServices.Instance);
    }
}
