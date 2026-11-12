using Aspire.Hosting;
using Aspire.Hosting.Testing;
using FabricPilot.BuildingBlocks.Tenancy;
using FabricPilot.Modules.Contracts.Domain.Contracts;
using FabricPilot.Modules.Contracts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FabricPilot.Integration.Tests;

/// <summary>
/// Records contracts in one tenant straight into the AppHost's database, through the module's own DbContext: no use
/// case records a contract yet, so the tests put them where the list reads them from.
/// </summary>
public static class ContractsTable
{
    public static async Task RecordAsync(AspireFixture aspire, string tenantId, IReadOnlyList<Contract> contracts, CancellationToken cancellationToken)
    {
        Ensure.That(aspire).IsNotNull();
        Ensure.That(contracts).IsNotNull();

        var connectionString = await aspire.App.GetConnectionStringAsync("database", cancellationToken)
            ?? throw new InvalidOperationException("The AppHost's database has no connection string.");
        await using var services = new ServiceCollection().AddLogging().AddTenancy().BuildServiceProvider();
        await using var scope = services.CreateAsyncScope();
        var options = new DbContextOptionsBuilder<ContractsDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", ContractsDbContext.Schema))
            .Options;

        using var tenant = TenantScope.Begin(tenantId);
        await using var context = new ContractsDbContext(options, scope.ServiceProvider);
        context.AddRange(contracts);
        await context.SaveChangesAsync(cancellationToken);
    }
}
