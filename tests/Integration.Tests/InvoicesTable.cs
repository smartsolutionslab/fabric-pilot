using Aspire.Hosting;
using Aspire.Hosting.Testing;
using FabricPilot.BuildingBlocks.Tenancy;
using FabricPilot.Modules.Billing.Domain.Invoices;
using FabricPilot.Modules.Billing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FabricPilot.Integration.Tests;

/// <summary>
/// Records invoices in one tenant straight into the AppHost's database, through the module's own DbContext: no use
/// case issues an invoice yet, so the tests put them where the list and the download read them from.
/// </summary>
public static class InvoicesTable
{
    public static async Task RecordAsync(AspireFixture aspire, string tenantId, IReadOnlyList<Invoice> invoices, CancellationToken cancellationToken)
    {
        Ensure.That(aspire).IsNotNull();
        Ensure.That(invoices).IsNotNull();

        var connectionString = await aspire.App.GetConnectionStringAsync("database", cancellationToken)
            ?? throw new InvalidOperationException("The AppHost's database has no connection string.");
        await using var services = new ServiceCollection().AddLogging().AddTenancy().BuildServiceProvider();
        await using var scope = services.CreateAsyncScope();
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", BillingDbContext.Schema))
            .Options;

        using var tenant = TenantScope.Begin(tenantId);
        await using var context = new BillingDbContext(options, scope.ServiceProvider);
        context.AddRange(invoices);
        await context.SaveChangesAsync(cancellationToken);
    }
}
