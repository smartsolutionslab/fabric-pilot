using Azure.Identity;
using Azure.Storage.Blobs;

namespace FabricPilot.Modules.Tickets.Infrastructure.Storage;

public static class BlobServiceClients
{
    /// <summary>
    /// A client for the connection Aspire gives the <c>blobs</c> resource, in either of its forms: Azurite's
    /// connection string locally, or, deployed, the storage account's blob endpoint, reached with the app's Azure
    /// identity rather than a key.
    /// </summary>
    public static BlobServiceClient Create(string connection)
    {
        Ensure.That(connection).IsNotNullOrWhiteSpace();

        return Uri.TryCreate(connection, UriKind.Absolute, out var endpoint)
            ? new BlobServiceClient(endpoint, new DefaultAzureCredential())
            : new BlobServiceClient(connection);
    }
}
