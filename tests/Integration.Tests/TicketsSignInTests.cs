using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;

namespace FabricPilot.Integration.Tests;

/// <summary>
/// Signing in per tenant: a customer (realm role customer-user, which customer-admin includes) raises and reads only
/// their own tenant's tickets, and staff (realm role staff, in no tenant) read every tenant's tickets on the one
/// endpoint mapped across tenants, and nothing on the customers' endpoints.
/// </summary>
public class TicketsSignInTests(AspireFixture aspire)
{
    private const string AllTenants = "/tickets/tickets/all-tenants?page=1&pageSize=100";

    private static readonly Uri Tickets = new("/tickets/tickets", UriKind.Relative);

    [Fact]
    public async Task Neither_tenant_sees_the_other_tenants_tickets()
    {
        using var tenantA = await aspire.CreateHostClientAsync("developer");
        using var tenantB = await aspire.CreateHostClientAsync("developer-b");
        var ticketA = await RaiseAsync(tenantA, $"Tenant A {Guid.CreateVersion7()}");
        var ticketB = await RaiseAsync(tenantB, $"Tenant B {Guid.CreateVersion7()}");

        var listedForA = Ids(await Json(tenantA, "/tickets/tickets?page=1&pageSize=100"));
        var listedForB = Ids(await Json(tenantB, "/tickets/tickets?page=1&pageSize=100"));
        using var bReadsA = await tenantB.GetAsync(new Uri($"/tickets/tickets/{ticketA}", UriKind.Relative), TestContext.Current.CancellationToken);
        using var aReadsB = await tenantA.GetAsync(new Uri($"/tickets/tickets/{ticketB}", UriKind.Relative), TestContext.Current.CancellationToken);

        listedForA.ShouldContain(ticketA);
        listedForA.ShouldNotContain(ticketB);
        listedForB.ShouldContain(ticketB);
        listedForB.ShouldNotContain(ticketA);
        bReadsA.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        aReadsB.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Staff_see_both_tenants_tickets()
    {
        using var tenantA = await aspire.CreateHostClientAsync("developer");
        using var tenantB = await aspire.CreateHostClientAsync("developer-b");
        var ticketA = await RaiseAsync(tenantA, $"Seen by staff A {Guid.CreateVersion7()}");
        var ticketB = await RaiseAsync(tenantB, $"Seen by staff B {Guid.CreateVersion7()}");
        using var staff = await aspire.CreateHostClientAsync("staff");

        var listed = await Json(staff, AllTenants);

        var ids = Ids(listed);
        ids.ShouldContain(ticketA);
        ids.ShouldContain(ticketB);
        listed["items"]!.AsArray().Single(item => item!["id"]!.GetValue<Guid>() == ticketB)!["subject"]!.GetValue<string>().ShouldStartWith("Seen by staff B");
    }

    [Fact]
    public async Task Staff_read_the_all_tenants_list_newest_first_unless_another_order_is_asked_for()
    {
        using var staff = await aspire.CreateHostClientAsync("staff");
        using var tenantA = await aspire.CreateHostClientAsync("developer");
        using var tenantB = await aspire.CreateHostClientAsync("developer-b");
        await RaiseAsync(tenantA, $"Ordered A {Guid.CreateVersion7()}");
        await RaiseAsync(tenantB, $"Ordered B {Guid.CreateVersion7()}");

        var newestFirst = RaisedAt(await Json(staff, AllTenants));
        var oldestFirst = RaisedAt(await Json(staff, $"{AllTenants}&sortBy=RaisedAt&sortDirection=Ascending"));

        newestFirst.Count.ShouldBeGreaterThan(1);
        newestFirst.ShouldBe([.. newestFirst.OrderDescending()]);
        oldestFirst.ShouldBe([.. oldestFirst.Order()]);
    }

    [Theory]
    [InlineData("developer")]
    [InlineData("developer-b")]
    [InlineData("visitor")]
    public async Task A_user_in_a_tenant_cannot_read_every_tenants_tickets(string user)
    {
        using var client = await aspire.CreateHostClientAsync(user);

        using var response = await client.GetAsync(new Uri(AllTenants, UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task An_anonymous_caller_cannot_read_every_tenants_tickets()
    {
        using var client = aspire.CreateHostClient();

        using var response = await client.GetAsync(new Uri(AllTenants, UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>developer holds customer-admin, which includes customer-user; developer-b holds customer-user.</summary>
    [Theory]
    [InlineData("developer")]
    [InlineData("developer-b")]
    public async Task A_customer_user_or_customer_admin_raises_and_reads_their_tenants_tickets(string user)
    {
        using var client = await aspire.CreateHostClientAsync(user);

        var id = await RaiseAsync(client, $"Customer {user} {Guid.CreateVersion7()}");
        using var ticket = await client.GetAsync(new Uri($"/tickets/tickets/{id}", UriKind.Relative), TestContext.Current.CancellationToken);

        ticket.StatusCode.ShouldBe(HttpStatusCode.OK);
        Ids(await Json(client, "/tickets/tickets?page=1&pageSize=100")).ShouldContain(id);
    }

    /// <summary>visitor is in tenant-a without a customer role; staff hold staff and are in no tenant.</summary>
    [Theory]
    [InlineData("visitor")]
    [InlineData("staff")]
    public async Task A_user_without_a_customer_role_cannot_raise_or_read_tickets(string user)
    {
        using var customer = await aspire.CreateHostClientAsync("developer");
        var id = await RaiseAsync(customer, $"Not for {user} {Guid.CreateVersion7()}", ("note.txt", Encoding.UTF8.GetBytes("note")));
        var attachment = (await Json(customer, $"/tickets/tickets/{id}"))["attachments"]![0]!["id"]!.GetValue<Guid>();
        using var client = await aspire.CreateHostClientAsync(user);

        using var form = Form($"From {user}");
        using var raised = await client.PostAsync(Tickets, form, TestContext.Current.CancellationToken);
        using var list = await client.GetAsync(new Uri("/tickets/tickets?page=1&pageSize=100", UriKind.Relative), TestContext.Current.CancellationToken);
        using var ticket = await client.GetAsync(new Uri($"/tickets/tickets/{id}", UriKind.Relative), TestContext.Current.CancellationToken);
        using var download = await client.GetAsync(new Uri($"/tickets/tickets/{id}/attachments/{attachment}", UriKind.Relative), TestContext.Current.CancellationToken);

        raised.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        list.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        ticket.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        download.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private static async Task<Guid> RaiseAsync(HttpClient client, string subject, params (string FileName, byte[] Content)[] files)
    {
        using var form = Form(subject, files);
        using var raised = await client.PostAsync(Tickets, form, TestContext.Current.CancellationToken);
        raised.StatusCode.ShouldBe(HttpStatusCode.Created, await raised.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return await raised.Content.ReadFromJsonAsync<Guid>(TestContext.Current.CancellationToken);
    }

    private static MultipartFormDataContent Form(string subject, params (string FileName, byte[] Content)[] files)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent(subject), "subject" },
            { new StringContent("Signed in per tenant."), "description" },
            { new StringContent("Normal"), "priority" },
        };
        foreach (var (fileName, content) in files)
        {
            form.Add(new ByteArrayContent(content), "attachments", fileName);
        }

        return form;
    }

    private static List<Guid> Ids(JsonObject list) =>
        [.. list["items"]!.AsArray().Select(item => item!["id"]!.GetValue<Guid>())];

    private static List<DateTimeOffset> RaisedAt(JsonObject list) =>
        [.. list["items"]!.AsArray().Select(item => item!["raisedAt"]!.GetValue<DateTimeOffset>())];

    private static async Task<JsonObject> Json(HttpClient client, string path)
    {
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, $"GET {path}");
        return (await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken))!;
    }
}
