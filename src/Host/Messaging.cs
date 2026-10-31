using FabricPilot.BuildingBlocks.Modules;
using Wolverine;
using Wolverine.EntityFrameworkCore;
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
