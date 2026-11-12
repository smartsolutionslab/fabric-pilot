using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FabricPilot.Modules.Tickets.Application;

namespace FabricPilot.Modules.Tickets.Infrastructure.Storage;

/// <summary>Ticket attachments in one blob container, created on first upload.</summary>
public sealed class AzureBlobStore(BlobServiceClient service) : IBlobStore
{
    public const string Container = "ticket-attachments";

    private readonly BlobContainerClient container = service.GetBlobContainerClient(Container);

    public async Task UploadAsync(string name, Stream content, CancellationToken cancellationToken)
    {
        await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
        await container.GetBlobClient(name).UploadAsync(content, overwrite: true, cancellationToken);
    }

    public async Task<Option<Stream>> OpenReadAsync(string name, CancellationToken cancellationToken)
    {
        try
        {
            var download = await container.GetBlobClient(name).DownloadStreamingAsync(cancellationToken: cancellationToken);
            return Option<Stream>.Some(download.Value.Content);
        }
        catch (RequestFailedException missing) when (missing.ErrorCode == BlobErrorCode.BlobNotFound || missing.ErrorCode == BlobErrorCode.ContainerNotFound)
        {
            return Option<Stream>.None;
        }
    }
}
