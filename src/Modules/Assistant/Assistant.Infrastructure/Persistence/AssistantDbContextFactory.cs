using FabricPilot.BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FabricPilot.Modules.Assistant.Infrastructure.Persistence;

/// <summary>Used by <c>dotnet ef</c> only: builds the context without the application or a database.</summary>
public sealed class AssistantDbContextFactory : IDesignTimeDbContextFactory<AssistantDbContext>
{
    public AssistantDbContext CreateDbContext(string[] args)
    {
        const string connectionString = "Server=localhost";
        const string schema = AssistantDbContext.Schema;
        var options = new DbContextOptionsBuilder<AssistantDbContext>();
        options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", schema));
        return new AssistantDbContext(options.Options, NoServices.Instance);
    }
}
