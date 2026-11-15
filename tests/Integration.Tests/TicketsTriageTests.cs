using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Aspire.Hosting.Testing;

namespace FabricPilot.Integration.Tests;

/// <summary>
/// The staff triage queue: staff filter every tenant's tickets by tenant, status and priority, and assign a ticket or
/// change its status in the ticket's own tenant, named in the route (<c>ForTenant()</c>). A status change reaches the
/// customer who raised the ticket, as a notice and an e-mail. No tenant's user triages, whatever their roles.
/// </summary>
public class TicketsTriageTests(AspireFixture aspire)
{
    private const string Queue = "/tickets/tickets/all-tenants?page=1&pageSize=100";

    private static readonly Uri Notices = new("/notifications/notices?page=1&pageSize=100", UriKind.Relative);

    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(60);

    [Fact]
    public async Task The_queue_shows_each_tickets_tenant_status_and_assignee_and_is_filtered_by_tenant()
    {
        using var tenantA = await aspire.CreateHostClientAsync("developer");
        using var tenantB = await aspire.CreateHostClientAsync("developer-b");
        var ticketA = await RaiseAsync(tenantA, $"Queue A {Guid.CreateVersion7()}", "Normal");
        var ticketB = await RaiseAsync(tenantB, $"Queue B {Guid.CreateVersion7()}", "Normal");
        using var staff = await aspire.CreateHostClientAsync("staff");

        var everyTenant = Items(await Json(staff, Queue));
        var onlyB = Items(await Json(staff, $"{Queue}&tenant=tenant-b"));

        var rowA = everyTenant.Single(item => Id(item) == ticketA);
        rowA["tenant"]!.GetValue<string>().ShouldBe("tenant-a");
        rowA["status"]!.GetValue<string>().ShouldBe("Open");
        rowA["assignee"].ShouldBeNull();
        everyTenant.Single(item => Id(item) == ticketB)["tenant"]!.GetValue<string>().ShouldBe("tenant-b");
        onlyB.Select(Id).ShouldContain(ticketB);
        onlyB.Select(Id).ShouldNotContain(ticketA);
        onlyB.ShouldAllBe(item => item["tenant"]!.GetValue<string>() == "tenant-b");
    }

    [Fact]
    public async Task The_queue_is_filtered_by_priority_and_by_status()
    {
        using var customer = await aspire.CreateHostClientAsync("developer");
        var urgent = await RaiseAsync(customer, $"Urgent {Guid.CreateVersion7()}", "Urgent");
        var low = await RaiseAsync(customer, $"Low {Guid.CreateVersion7()}", "Low");
        using var staff = await aspire.CreateHostClientAsync("staff");
        (await ChangeStatusAsync(staff, "tenant-a", low, "Closed")).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var urgentOnes = Items(await Json(staff, $"{Queue}&priority=Urgent"));
        var closedOnes = Items(await Json(staff, $"{Queue}&status=Closed"));

        urgentOnes.Select(Id).ShouldContain(urgent);
        urgentOnes.Select(Id).ShouldNotContain(low);
        urgentOnes.ShouldAllBe(item => item["priority"]!.GetValue<string>() == "Urgent");
        closedOnes.Select(Id).ShouldContain(low);
        closedOnes.Select(Id).ShouldNotContain(urgent);
        closedOnes.ShouldAllBe(item => item["status"]!.GetValue<string>() == "Closed");
    }

    [Theory]
    [InlineData("status=Done")]
    [InlineData("priority=Critical")]
    public async Task A_filter_value_that_is_not_one_of_the_named_ones_is_a_bad_request(string filter)
    {
        using var staff = await aspire.CreateHostClientAsync("staff");

        using var response = await staff.GetAsync(new Uri($"{Queue}&{filter}", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Staff_change_a_tickets_status_and_the_customer_who_raised_it_gets_a_notice_and_an_email()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var customer = await aspire.CreateHostClientAsync("developer");
        var subject = $"Status change {Guid.CreateVersion7()}";
        var ticket = await RaiseAsync(customer, subject, "High");
        using var staff = await aspire.CreateHostClientAsync("staff");

        using var response = await ChangeStatusAsync(staff, "tenant-a", ticket, "InProgress");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync(cancellationToken));
        (await Json(staff, $"/tickets/tickets/all-tenants/{ticket}"))["status"]!.GetValue<string>().ShouldBe("InProgress");
        var notice = await Eventually(
            async () => (await Json(customer, Notices))["items"]!.AsArray().Select(item => item!.AsObject()).SingleOrDefault(item => item["ticketId"]!.GetValue<Guid>() == ticket),
            cancellationToken);
        notice["text"]!.GetValue<string>().ShouldBe($"Your ticket \"{subject}\" is now in progress.");
        var email = await Eventually(async () => (await EmailsAbout(subject, cancellationToken)).SingleOrDefault(), cancellationToken);
        email["To"]!.AsArray().Select(address => address!["Address"]!.GetValue<string>()).ShouldBe(["developer@example.com"]);
        email["Subject"]!.GetValue<string>().ShouldBe($"Your ticket \"{subject}\" is now in progress");
    }

    [Fact]
    public async Task Staff_assign_a_ticket_to_themselves_and_then_to_a_colleague()
    {
        using var customer = await aspire.CreateHostClientAsync("developer-b");
        var ticket = await RaiseAsync(customer, $"Assigned {Guid.CreateVersion7()}", "Normal");
        using var staff = await aspire.CreateHostClientAsync("staff");

        using var toThemselves = await AssignAsync(staff, "tenant-b", ticket, "staff");
        var afterFirst = await Json(staff, $"/tickets/tickets/all-tenants/{ticket}");
        using var toColleague = await AssignAsync(staff, "tenant-b", ticket, "colleague");

        toThemselves.StatusCode.ShouldBe(HttpStatusCode.NoContent, await toThemselves.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        afterFirst["assignee"]!.GetValue<string>().ShouldBe("staff");
        afterFirst["tenant"]!.GetValue<string>().ShouldBe("tenant-b");
        toColleague.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        Items(await Json(staff, $"{Queue}&tenant=tenant-b")).Single(item => Id(item) == ticket)["assignee"]!.GetValue<string>().ShouldBe("colleague");
    }

    [Fact]
    public async Task A_change_that_names_another_tenant_than_the_tickets_finds_no_ticket_and_changes_nothing()
    {
        using var customer = await aspire.CreateHostClientAsync("developer");
        var ticket = await RaiseAsync(customer, $"Wrong tenant {Guid.CreateVersion7()}", "Normal");
        using var staff = await aspire.CreateHostClientAsync("staff");

        using var status = await ChangeStatusAsync(staff, "tenant-b", ticket, "Closed");
        using var assignee = await AssignAsync(staff, "tenant-b", ticket, "staff");

        status.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ProblemCodeAsync(status)).ShouldBe("tickets.ticket-not-found");
        assignee.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var unchanged = await Json(staff, $"/tickets/tickets/all-tenants/{ticket}");
        unchanged["status"]!.GetValue<string>().ShouldBe("Open");
        unchanged["assignee"].ShouldBeNull();
    }

    [Fact]
    public async Task A_tenant_the_host_does_not_know_is_not_found()
    {
        using var staff = await aspire.CreateHostClientAsync("staff");

        using var status = await ChangeStatusAsync(staff, "tenant-aa", Guid.CreateVersion7(), "Closed");
        using var assignee = await AssignAsync(staff, "tenant-aa", Guid.CreateVersion7(), "staff");

        status.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ProblemCodeAsync(status)).ShouldBe("tenant-not-found");
        assignee.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ProblemCodeAsync(assignee)).ShouldBe("tenant-not-found");
    }

    [Fact]
    public async Task A_status_that_is_not_one_of_the_named_ones_is_refused_with_its_code()
    {
        using var customer = await aspire.CreateHostClientAsync("developer");
        var ticket = await RaiseAsync(customer, $"Unknown status {Guid.CreateVersion7()}", "Normal");
        using var staff = await aspire.CreateHostClientAsync("staff");

        using var response = await ChangeStatusAsync(staff, "tenant-a", ticket, "Done");

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await ProblemCodeAsync(response)).ShouldBe("tickets.unknown-status");
    }

    /// <summary>developer holds customer-admin and manager in tenant-a, developer-b customer-user in tenant-b, visitor no role.</summary>
    [Theory]
    [InlineData("developer")]
    [InlineData("developer-b")]
    [InlineData("visitor")]
    public async Task A_tenants_user_is_forbidden_to_triage_whatever_their_roles_even_in_their_own_tenant(string user)
    {
        using var owner = await aspire.CreateHostClientAsync(user == "developer-b" ? "developer-b" : "developer");
        var tenant = user == "developer-b" ? "tenant-b" : "tenant-a";
        var ticket = await RaiseAsync(owner, $"Not triaged by {user} {Guid.CreateVersion7()}", "Normal");
        using var client = await aspire.CreateHostClientAsync(user);

        using var status = await ChangeStatusAsync(client, tenant, ticket, "Closed");
        using var assignee = await AssignAsync(client, tenant, ticket, user);
        using var queue = await client.GetAsync(new Uri(Queue, UriKind.Relative), TestContext.Current.CancellationToken);

        status.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        assignee.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        queue.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task An_anonymous_caller_is_unauthorized_to_triage()
    {
        using var client = aspire.CreateHostClient();

        using var status = await ChangeStatusAsync(client, "tenant-a", Guid.CreateVersion7(), "Closed");
        using var assignee = await AssignAsync(client, "tenant-a", Guid.CreateVersion7(), "staff");

        status.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        assignee.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static Task<HttpResponseMessage> ChangeStatusAsync(HttpClient client, string tenant, Guid ticket, string status) =>
        client.PutAsJsonAsync(new Uri($"/tickets/tickets/{tenant}/{ticket}/status", UriKind.Relative), new { status }, TestContext.Current.CancellationToken);

    private static Task<HttpResponseMessage> AssignAsync(HttpClient client, string tenant, Guid ticket, string assignee) =>
        client.PutAsJsonAsync(new Uri($"/tickets/tickets/{tenant}/{ticket}/assignee", UriKind.Relative), new { assignee }, TestContext.Current.CancellationToken);

    private static async Task<Guid> RaiseAsync(HttpClient client, string subject, string priority)
    {
        using var form = new MultipartFormDataContent
        {
            { new StringContent(subject), "subject" },
            { new StringContent("Triaged by staff."), "description" },
            { new StringContent(priority), "priority" },
        };
        using var raised = await client.PostAsync(new Uri("/tickets/tickets", UriKind.Relative), form, TestContext.Current.CancellationToken);
        raised.StatusCode.ShouldBe(HttpStatusCode.Created, await raised.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return await raised.Content.ReadFromJsonAsync<Guid>(TestContext.Current.CancellationToken);
    }

    private static async Task<JsonObject> Json(HttpClient client, string path)
    {
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, $"GET {path}");
        return (await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken))!;
    }

    private static Task<JsonObject> Json(HttpClient client, Uri path) => Json(client, path.OriginalString);

    private static List<JsonObject> Items(JsonObject page) => [.. page["items"]!.AsArray().Select(item => item!.AsObject())];

    private static Guid Id(JsonObject item) => item["id"]!.GetValue<Guid>();

    private static async Task<string?> ProblemCodeAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken))?["code"]?.GetValue<string>();

    private static async Task<T> Eventually<T>(Func<Task<T?>> attempt, CancellationToken cancellationToken)
        where T : class
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Patience);
        while (true)
        {
            if (await attempt() is { } found)
            {
                return found;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), timeout.Token);
        }
    }

    /// <summary>The messages Mailpit caught whose subject names this ticket, through its API.</summary>
    private async Task<List<JsonObject>> EmailsAbout(string ticketSubject, CancellationToken cancellationToken)
    {
        using var mailpit = new HttpClient { BaseAddress = aspire.App.GetEndpoint("mailpit", "http") };
        var query = Uri.EscapeDataString($"subject:\"{ticketSubject}\"");
        var found = await mailpit.GetFromJsonAsync<JsonObject>(new Uri($"/api/v1/search?query={query}", UriKind.Relative), cancellationToken);
        return [.. found!["messages"]!.AsArray().Select(message => message!.AsObject())];
    }
}
