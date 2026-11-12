using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

namespace FabricPilot.Integration.Tests;

/// <summary>The AppHost's blob storage as a developer runs it, read from the application model without starting it.</summary>
public class BlobStorageTests
{
    /// <summary>
    /// PostgreSQL keeps a ticket in its volume, so Azurite must keep the ticket's files in one too: otherwise a restarted
    /// AppHost lists attachments whose download is a 404.
    /// </summary>
    [Fact]
    public async Task The_blob_storage_keeps_its_files_in_a_volume_as_the_database_keeps_its_rows()
    {
        await using var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>(TestContext.Current.CancellationToken);

        var storage = builder.Resources.Single(resource => resource.Name == "storage");

        storage.Annotations.OfType<ContainerMountAnnotation>().ShouldContain(mount => mount.Type == ContainerMountType.Volume);
    }
}
