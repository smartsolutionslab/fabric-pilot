namespace FabricPilot.Modules.Tickets.Application;

/// <summary>
/// Files kept by name outside the database: Azurite locally, Azure Blob Storage when deployed. Implemented in the
/// module's Infrastructure project, and the one place a blob storage pack would replace. A name is the current tenant's:
/// the store keeps every file under that tenant, so the same name opens nothing for another, and nothing without one.
/// </summary>
public interface IBlobStore
{
    /// <summary>Stores the content under the name, replacing what was stored there; fails without a tenant.</summary>
    Task UploadAsync(string name, Stream content, CancellationToken cancellationToken);

    /// <summary>The content stored under the name, for the caller to read and dispose; none when nothing is.</summary>
    Task<Option<Stream>> OpenReadAsync(string name, CancellationToken cancellationToken);
}
