using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Azure.Storage.Blobs;
using FabricPilot.Modules.Tickets.Infrastructure.Persistence;
using FabricPilot.Modules.Tickets.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FabricPilot.Integration.Tests;

/// <summary>
/// Attachments stored before their blob names carried the tenant, <c>{ticket}/{attachment}</c>, against the AppHost's
/// database and Azurite: the MigrationRunner's step moves each under its ticket's tenant, so it is still served.
/// </summary>
public class LegacyAttachmentNamesTests(AspireFixture aspire)
{
    private static readonly Uri Tickets = new("/tickets/tickets", UriKind.Relative);

    [Fact]
    public async Task An_attachment_stored_under_its_old_name_moves_under_its_tenant_and_still_downloads()
    {
        var content = Encoding.UTF8.GetBytes("stored before the tenant prefix");
        using var client = await aspire.CreateHostClientAsync("developer");
        var (ticket, attachment) = await RaiseAsync(client, content);
        var container = (await BlobServiceAsync()).GetBlobContainerClient(AzureBlobStore.Container);
        var legacy = container.GetBlobClient($"{ticket}/{attachment}");
        var current = container.GetBlobClient($"tenant-a/{ticket}/{attachment}");
        await legacy.UploadAsync(BinaryData.FromBytes(content), overwrite: true, TestContext.Current.CancellationToken);
        await current.DeleteIfExistsAsync(cancellationToken: TestContext.Current.CancellationToken);

        await MigrateAsync();
        await MigrateAsync();

        (await legacy.ExistsAsync(TestContext.Current.CancellationToken)).Value.ShouldBeFalse();
        (await current.ExistsAsync(TestContext.Current.CancellationToken)).Value.ShouldBeTrue();
        using var download = await client.GetAsync(new Uri($"/tickets/tickets/{ticket}/attachments/{attachment}", UriKind.Relative), TestContext.Current.CancellationToken);
        download.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await download.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken)).ShouldBe(content);
    }

    /// <summary>Runs the step as the MigrationRunner does: with the module's DbContext and no tenant.</summary>
    private async Task MigrateAsync()
    {
        var database = await aspire.App.GetConnectionStringAsync("database", TestContext.Current.CancellationToken)
            ?? throw new InvalidOperationException("The AppHost's database has no connection string.");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:blobs"] = await BlobsAsync() })
            .Build();
        await using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        var options = new DbContextOptionsBuilder<TicketsDbContext>()
            .UseNpgsql(database, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", TicketsDbContext.Schema))
            .Options;
        await using var tickets = new TicketsDbContext(options, services);

        await new LegacyAttachmentNames(tickets, configuration).RunAsync(TestContext.Current.CancellationToken);
    }

    private async Task<BlobServiceClient> BlobServiceAsync() => new(await BlobsAsync());

    private async Task<string> BlobsAsync() =>
        await aspire.App.GetConnectionStringAsync("blobs", TestContext.Current.CancellationToken)
            ?? throw new InvalidOperationException("The AppHost's blob storage has no connection string.");

    /// <summary>Raises a ticket with one attachment through the host; its id and the attachment's.</summary>
    private static async Task<(Guid Ticket, Guid Attachment)> RaiseAsync(HttpClient client, byte[] content)
    {
        using var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        using var form = new MultipartFormDataContent
        {
            { new StringContent($"Old blob name {Guid.CreateVersion7()}"), "subject" },
            { new StringContent("Steps to reproduce: open the file."), "description" },
            { new StringContent("Low"), "priority" },
            { file, "attachments", "old.txt" },
        };
        using var raised = await client.PostAsync(Tickets, form, TestContext.Current.CancellationToken);
        raised.StatusCode.ShouldBe(HttpStatusCode.Created, await raised.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var ticket = await raised.Content.ReadFromJsonAsync<Guid>(TestContext.Current.CancellationToken);

        var read = await client.GetFromJsonAsync<JsonObject>(new Uri($"/tickets/tickets/{ticket}", UriKind.Relative), TestContext.Current.CancellationToken);
        return (ticket, read!["attachments"]![0]!["id"]!.GetValue<Guid>());
    }
}
