using FabricPilot.BuildingBlocks.Modules;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.ErrorHandling;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

namespace Microsoft.Extensions.DependencyInjection;

public static class Messaging
{
    /// <summary>Wolverine with a durable outbox and inbox in the project's database, the broker as transport, and every module's consumers.</summary>
    public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration, IReadOnlyList<IModule> modules)
    {
        Ensure.That(configuration).IsNotNull();
        Ensure.That(modules).IsNotNull();

        var database = configuration.GetConnectionString("database")
            ?? throw new InvalidOperationException("Connection string 'database' is missing; Aspire provides it.");
        var broker = configuration.GetConnectionString("messaging")
            ?? throw new InvalidOperationException("Connection string 'messaging' is missing; Aspire provides it.");

        services.AddWolverine(options =>
        {
            // Handler code is generated at startup. For production, pre-generate it (see docs/adr).
            options.UseRuntimeCompilation();
            options.PersistMessagesWithPostgresql(database, "wolverine");
            options.UseEntityFrameworkCoreTransactions();
            options.Policies.UseDurableOutboxOnAllSendingEndpoints();
            options.Policies.UseDurableInboxOnAllListeners();
            // A consumer that throws is tried again, the wait doubling from 1 second, before its message is dead-lettered
            // (seven attempts, about a minute; see docs/adr). The retry is scheduled in the durable inbox, so the
            // listener delivers other messages meanwhile.
            options.OnAnyException().ScheduleRetry(
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(4),
                TimeSpan.FromSeconds(8),
                TimeSpan.FromSeconds(16),
                TimeSpan.FromSeconds(32));
            options.UseRabbitMq(new Uri(broker)).AutoProvision().UseConventionalRouting();

            options.Discovery.DisableConventionalDiscovery();
            options.Discovery.CustomizeHandlerDiscovery(query => query.Includes.WithNameSuffix("Consumer"));
            foreach (var module in modules)
            {
                options.Discovery.IncludeAssembly(ModuleAssemblies.Of(module, "Application"));
            }
        });

        return services;
    }
}
