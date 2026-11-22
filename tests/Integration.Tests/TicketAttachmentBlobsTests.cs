using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using FabricPilot.BuildingBlocks.Tenancy;
using FabricPilot.Modules.Tickets.Application;
using FabricPilot.Modules.Tickets.Domain.Tickets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FabricPilot.Integration.Tests;

/// <summary>
/// The attachments' blob store itself, against the AppHost's Azurite, with the ticket lookup that runs before it in
/// the host left out: a blob's name opens a file only for the tenant that stored it.
/// </summary>
public class TicketAttachmentBlobsTests(AspireFixture aspire)
{
    private static readonly Uri Tickets = new("/tickets/tickets", UriKind.Relative);

    [Fact]
    public async Task Another_tenants_attachment_is_not_opened_by_its_blob_name()
    {
        var content = Encoding.UTF8.GetBytes("tenant a only");
        var (ticket, attachment) = await RaiseAsync("developer", content);
        var name = AttachmentBlobs.Name(new TicketId(ticket), new AttachmentId(attachment));
        await using var services = await BlobStoreServicesAsync();

        var ownTenant = await OpenAsync(services, "tenant-a", name);
        var otherTenant = await OpenAsync(services, "tenant-b", name);

        ownTenant.ShouldBe(content);
        otherTenant.ShouldBeNull($"tenant-b opened tenant-a's attachment by its blob name '{name}'");
    }

    /// <summary>The services the host registers for the store, outside a request: no ticket is looked up first.</summary>
    private async Task<ServiceProvider> BlobStoreServicesAsync()
    {
        var blobs = await aspire.App.GetConnectionStringAsync("blobs", TestContext.Current.CancellationToken)
            ?? throw new InvalidOperationException("The AppHost's blob storage has no connection string.");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:blobs"] = blobs })
            .Build();
        return new ServiceCollection().AddLogging().AddTenancy().AddTicketsStorage(configuration).BuildServiceProvider();
    }

    /// <summary>What the store opens under the name within the tenant, or null when it opens nothing.</summary>
    private static async Task<byte[]?> OpenAsync(ServiceProvider services, string tenantId, string name)
    {
        using var tenant = TenantScope.Begin(tenantId);
        await using var scope = services.CreateAsyncScope();
        var opened = await scope.ServiceProvider.GetRequiredService<IBlobStore>().OpenReadAsync(name, TestContext.Current.CancellationToken);
        if (!opened.HasValue)
        {
            return null;
        }

        await using var stream = opened.Value;
        using var copy = new MemoryStream();
        await stream.CopyToAsync(copy, TestContext.Current.CancellationToken);
        return copy.ToArray();
    }

    /// <summary>Raises a ticket with one attachment as the user, through the host; its id and the attachment's.</summary>
    private async Task<(Guid Ticket, Guid Attachment)> RaiseAsync(string user, byte[] content)
    {
        using var client = await aspire.CreateHostClientAsync(user);
        using var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        using var form = new MultipartFormDataContent
        {
            { new StringContent($"Blob names {Guid.CreateVersion7()}"), "subject" },
            { new StringContent("Steps to reproduce: open the file."), "description" },
            { new StringContent("Low"), "priority" },
            { file, "attachments", "secret.txt" },
        };
        using var raised = await client.PostAsync(Tickets, form, TestContext.Current.CancellationToken);
        raised.StatusCode.ShouldBe(HttpStatusCode.Created, await raised.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var ticket = await raised.Content.ReadFromJsonAsync<Guid>(TestContext.Current.CancellationToken);

        var read = await client.GetFromJsonAsync<JsonObject>(new Uri($"/tickets/tickets/{ticket}", UriKind.Relative), TestContext.Current.CancellationToken);
        return (ticket, read!["attachments"]![0]!["id"]!.GetValue<Guid>());
    }
}
