using FabricPilot.Modules.Tickets.Application;
using FabricPilot.Modules.Tickets.Infrastructure;
using FabricPilot.Modules.Tickets.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class TicketsStorage
{
    private const string BlobsConnectionName = "blobs";

    /// <summary>The tickets table and the attachments' blob storage, whose connection Aspire provides as <c>blobs</c>.</summary>
    public static IServiceCollection AddTicketsStorage(this IServiceCollection services, IConfiguration configuration)
    {
        Ensure.That(configuration).IsNotNull();

        return services
            .AddScoped<ITicketStore, TicketStore>()
            .AddSingleton(_ => BlobServiceClients.Create(configuration.GetConnectionString(BlobsConnectionName)
                ?? throw new InvalidOperationException($"Connection string '{BlobsConnectionName}' is missing; Aspire provides it.")))
            .AddSingleton<IBlobStore, AzureBlobStore>();
    }
}
