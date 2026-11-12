namespace FabricPilot.Modules.Tickets.Application;

/// <summary>
/// Files kept by name outside the database: Azurite locally, Azure Blob Storage when deployed. Implemented in the
/// module's Infrastructure project, and the one place a blob storage pack would replace.
/// </summary>
public interface IBlobStore
{
    /// <summary>Stores the content under the name, replacing what was stored there.</summary>
    Task UploadAsync(string name, Stream content, CancellationToken cancellationToken);

    /// <summary>The content stored under the name, for the caller to read and dispose; none when nothing is.</summary>
    Task<Option<Stream>> OpenReadAsync(string name, CancellationToken cancellationToken);
}
