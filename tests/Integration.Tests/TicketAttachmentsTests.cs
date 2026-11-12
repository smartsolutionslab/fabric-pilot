using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;

namespace FabricPilot.Integration.Tests;

/// <summary>
/// Raising a ticket with attachments through the host, against the AppHost's database and its Azurite blob storage:
/// the files go to blob storage and come back unchanged, only within the tenant that raised the ticket.
/// </summary>
public class TicketAttachmentsTests(AspireFixture aspire)
{
    private const long SizeLimit = 10 * 1024 * 1024;

    private static readonly Uri Tickets = new("/tickets/tickets", UriKind.Relative);

    [Fact]
    public async Task A_ticket_raised_with_two_attachments_is_listed_and_its_attachments_download_unchanged()
    {
        using var client = await aspire.CreateHostClientAsync("developer");
        var subject = $"Printer offline {Guid.CreateVersion7()}";
        var screenshot = Bytes(70_000);
        var log = Encoding.UTF8.GetBytes("2026-11-12 09:30 error 42\n");

        using var form = Form(subject, "High", ("screenshot.png", "image/png", screenshot), ("app.log", "text/plain", log));
        using var raised = await client.PostAsync(Tickets, form, TestContext.Current.CancellationToken);

        raised.StatusCode.ShouldBe(HttpStatusCode.Created, await raised.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var id = await raised.Content.ReadFromJsonAsync<Guid>(TestContext.Current.CancellationToken);
        raised.Headers.Location.ShouldNotBeNull().OriginalString.ShouldBe($"/tickets/tickets/{id}");

        var list = await Json(client, "/tickets/tickets?page=1&pageSize=100");
        var listed = list["items"]!.AsArray().Where(item => item!["id"]!.GetValue<Guid>() == id).ShouldHaveSingleItem()!;
        listed["subject"]!.GetValue<string>().ShouldBe(subject);
        listed["priority"]!.GetValue<string>().ShouldBe("High");
        listed["attachmentCount"]!.GetValue<int>().ShouldBe(2);

        var ticket = await Json(client, $"/tickets/tickets/{id}");
        var attachments = ticket["attachments"]!.AsArray();
        attachments.Select(attachment => attachment!["fileName"]!.GetValue<string>()).ShouldBe(["screenshot.png", "app.log"]);
        attachments.Select(attachment => attachment!["size"]!.GetValue<long>()).ShouldBe([screenshot.LongLength, log.LongLength]);

        foreach (var (attachment, expected) in attachments.Zip([screenshot, log]))
        {
            using var download = await client.GetAsync(new Uri($"/tickets/tickets/{id}/attachments/{attachment!["id"]!.GetValue<Guid>()}", UriKind.Relative), TestContext.Current.CancellationToken);

            download.StatusCode.ShouldBe(HttpStatusCode.OK);
            download.Content.Headers.ContentDisposition.ShouldNotBeNull().DispositionType.ShouldBe("attachment");
            download.Content.Headers.ContentDisposition.FileNameStar.ShouldBe(attachment["fileName"]!.GetValue<string>());
            (await download.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken)).ShouldBe(expected);
        }
    }

    [Fact]
    public async Task An_attachment_over_the_size_limit_is_refused_with_its_code()
    {
        using var client = await aspire.CreateHostClientAsync("developer");
        var subject = $"Too big {Guid.CreateVersion7()}";

        using var form = Form(subject, "Normal", ("disk.img", "application/octet-stream", new byte[SizeLimit + 1]));
        using var response = await client.PostAsync(Tickets, form, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);
        problem!["code"]!.GetValue<string>().ShouldBe("tickets.attachment-too-large");
        var list = await Json(client, "/tickets/tickets?page=1&pageSize=100");
        list["items"]!.AsArray().ShouldNotContain(item => item!["subject"]!.GetValue<string>() == subject);
    }

    [Fact]
    public async Task A_ticket_with_the_most_attachments_each_of_the_largest_size_is_raised()
    {
        using var client = await aspire.CreateHostClientAsync("developer");
        var files = Enumerable.Range(1, 5).Select(number => ($"part{number}.bin", "application/octet-stream", new byte[SizeLimit])).ToArray();

        using var form = Form($"Largest {Guid.CreateVersion7()}", "Normal", files);
        using var response = await client.PostAsync(Tickets, form, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Another_tenants_ticket_and_its_attachments_are_not_found()
    {
        using var tenantA = await aspire.CreateHostClientAsync("developer");
        using var tenantB = await aspire.CreateHostClientAsync("developer-b");
        using var form = Form($"Tenant A only {Guid.CreateVersion7()}", "Low", ("secret.txt", "text/plain", Encoding.UTF8.GetBytes("tenant a")));
        using var raised = await tenantA.PostAsync(Tickets, form, TestContext.Current.CancellationToken);
        raised.StatusCode.ShouldBe(HttpStatusCode.Created);
        var id = await raised.Content.ReadFromJsonAsync<Guid>(TestContext.Current.CancellationToken);
        var attachment = (await Json(tenantA, $"/tickets/tickets/{id}"))["attachments"]![0]!["id"]!.GetValue<Guid>();

        using var ticket = await tenantB.GetAsync(new Uri($"/tickets/tickets/{id}", UriKind.Relative), TestContext.Current.CancellationToken);
        using var download = await tenantB.GetAsync(new Uri($"/tickets/tickets/{id}/attachments/{attachment}", UriKind.Relative), TestContext.Current.CancellationToken);
        var list = await Json(tenantB, "/tickets/tickets?page=1&pageSize=100");

        ticket.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        download.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        list["items"]!.AsArray().ShouldNotContain(item => item!["id"]!.GetValue<Guid>() == id);
    }

    [Fact]
    public async Task An_anonymous_caller_cannot_raise_a_ticket()
    {
        using var client = aspire.CreateHostClient();
        using var form = Form("Anonymous", "Low");

        using var response = await client.PostAsync(Tickets, form, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static MultipartFormDataContent Form(string subject, string priority, params (string FileName, string ContentType, byte[] Content)[] files)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent(subject), "subject" },
            { new StringContent("Steps to reproduce: switch it on."), "description" },
            { new StringContent(priority), "priority" },
        };
        foreach (var (fileName, contentType, content) in files)
        {
            var file = new ByteArrayContent(content);
            file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            form.Add(file, "attachments", fileName);
        }

        return form;
    }

    /// <summary>Every byte value, over and over: a file whose content a lossy round trip would change.</summary>
    private static byte[] Bytes(int length) => [.. Enumerable.Range(0, length).Select(index => (byte)(index % 256))];

    private static async Task<JsonObject> Json(HttpClient client, string path)
    {
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, $"GET {path}");
        return (await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken))!;
    }
}
