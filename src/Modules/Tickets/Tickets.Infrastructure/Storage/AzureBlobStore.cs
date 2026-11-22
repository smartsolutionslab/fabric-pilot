using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FabricPilot.BuildingBlocks.Tenancy;
using FabricPilot.Modules.Tickets.Application;

namespace FabricPilot.Modules.Tickets.Infrastructure.Storage;

/// <summary>
/// Ticket attachments in one blob container, created on first upload. Every name is stored under the current tenant,
/// read from <see cref="ITenantContext"/> and never from the caller, so a name opens only the tenant's own files,
/// whatever was looked up before: without a tenant nothing is opened and nothing is stored.
/// </summary>
public sealed class AzureBlobStore(BlobServiceClient service, ITenantContext tenants) : IBlobStore
{
    public const string Container = "ticket-attachments";

    private readonly BlobContainerClient container = service.GetBlobContainerClient(Container);

    /// <summary>Where the tenant's blob of that name is kept in the container.</summary>
    public static string TenantName(string tenant, string name)
    {
        Ensure.That(tenant).IsNotNullOrWhiteSpace();
        Ensure.That(name).IsNotNullOrWhiteSpace();

        return $"{tenant}/{name}";
    }

    public async Task UploadAsync(string name, Stream content, CancellationToken cancellationToken)
    {
        var tenant = tenants.TenantId ?? throw new InvalidOperationException($"Cannot store '{name}' without a tenant.");
        await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
        await container.GetBlobClient(TenantName(tenant, name)).UploadAsync(content, overwrite: true, cancellationToken);
    }

    public async Task<Option<Stream>> OpenReadAsync(string name, CancellationToken cancellationToken)
    {
        if (tenants.TenantId is not { } tenant)
        {
            return Option<Stream>.None;
        }

        try
        {
            var download = await container.GetBlobClient(TenantName(tenant, name)).DownloadStreamingAsync(cancellationToken: cancellationToken);
            return Option<Stream>.Some(download.Value.Content);
        }
        catch (RequestFailedException missing) when (missing.ErrorCode == BlobErrorCode.BlobNotFound || missing.ErrorCode == BlobErrorCode.ContainerNotFound)
        {
            return Option<Stream>.None;
        }
    }
}
