using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using FabricPilot.Contracts;
using FabricPilot.Contracts.Tickets;

namespace FabricPilot.Integration.Tests;

/// <summary>
/// A ticket's status change, published on the AppHost's broker, reaches its customer: one e-mail at the mail catcher
/// (Mailpit) however often it is delivered, and one notice that only that customer sees, in their tenant.
/// </summary>
public class NotificationsTests(AspireFixture aspire)
{
    private static readonly Uri Notices = new("/notifications/notices?page=1&pageSize=100", UriKind.Relative);

    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(60);

    [Fact]
    public async Task A_status_change_delivered_twice_emails_the_customer_once_and_shows_them_one_notice()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var customer = await aspire.CreateHostClientAsync("developer");
        var ticketId = Guid.CreateVersion7();
        var subject = $"Printer offline {ticketId}";
        var changed = new TicketStatusChangedV1(ticketId, subject, "Open", "Resolved", Subject(customer), "developer@example.com")
        {
            Metadata = new EventMetadata(Guid.CreateVersion7(), TimeProvider.System.GetUtcNow(), "tenant-a"),
        };

        await using (var publisher = await TicketsEventPublisher.StartAsync(await Broker(cancellationToken), cancellationToken))
        {
            await publisher.PublishAsync(changed);
            await publisher.PublishAsync(changed);

            var notice = await Eventually(async () => NoticesFor(await Json(customer, Notices), ticketId).SingleOrDefault(), cancellationToken);
            notice["text"]!.GetValue<string>().ShouldBe($"Your ticket \"{subject}\" is now Resolved.");
        }

        var emails = await Eventually(async () => await EmailsAbout(subject, cancellationToken) is { Count: > 0 } found ? found : null, cancellationToken);
        // The second delivery has had time to arrive; it must add nothing.
        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        emails = await EmailsAbout(subject, cancellationToken);
        var email = emails.ShouldHaveSingleItem()!;
        email["To"]!.AsArray().Select(address => address!["Address"]!.GetValue<string>()).ShouldBe(["developer@example.com"]);
        email["Subject"]!.GetValue<string>().ShouldBe($"Your ticket \"{subject}\" is now Resolved");
        NoticesFor(await Json(customer, Notices), ticketId).ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Another_customer_sees_none_of_a_customers_notices()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var customer = await aspire.CreateHostClientAsync("developer");
        using var otherTenant = await aspire.CreateHostClientAsync("developer-b");
        var ticketId = Guid.CreateVersion7();
        var changed = new TicketStatusChangedV1(ticketId, $"Mine only {ticketId}", "Open", "InProgress", Subject(customer), "developer@example.com")
        {
            Metadata = new EventMetadata(Guid.CreateVersion7(), TimeProvider.System.GetUtcNow(), "tenant-a"),
        };

        await using (var publisher = await TicketsEventPublisher.StartAsync(await Broker(cancellationToken), cancellationToken))
        {
            await publisher.PublishAsync(changed);
            await Eventually(async () => NoticesFor(await Json(customer, Notices), ticketId).SingleOrDefault(), cancellationToken);
        }

        NoticesFor(await Json(otherTenant, Notices), ticketId).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("visitor")]
    [InlineData("staff")]
    public async Task A_signed_in_user_without_the_customer_user_role_is_forbidden(string user)
    {
        using var client = await aspire.CreateHostClientAsync(user);

        using var response = await client.GetAsync(Notices, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task An_anonymous_caller_is_unauthorized()
    {
        using var client = aspire.CreateHostClient();

        using var response = await client.GetAsync(Notices, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>The Keycloak user id (<c>sub</c>) in the client's access token: whom a ticket's events name as its customer.</summary>
    private static string Subject(HttpClient client)
    {
        var payload = client.DefaultRequestHeaders.Authorization!.Parameter!.Split('.')[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');
        using var claims = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
        return claims.RootElement.GetProperty("sub").GetString()!;
    }

    private static IEnumerable<JsonObject> NoticesFor(JsonObject page, Guid ticketId) =>
        page["items"]!.AsArray().Select(item => item!.AsObject()).Where(item => item["ticketId"]!.GetValue<Guid>() == ticketId);

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

    private static async Task<JsonObject> Json(HttpClient client, Uri path)
    {
        using var response = await client.GetAsync(path, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, $"GET {path}");
        return (await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken))!;
    }

    private async Task<string> Broker(CancellationToken cancellationToken) =>
        await aspire.App.GetConnectionStringAsync("messaging", cancellationToken)
            ?? throw new InvalidOperationException("The AppHost gave no connection string for the broker.");

    /// <summary>The messages Mailpit caught whose subject names this ticket, through its API.</summary>
    private async Task<List<JsonObject>> EmailsAbout(string ticketSubject, CancellationToken cancellationToken)
    {
        using var mailpit = new HttpClient { BaseAddress = aspire.App.GetEndpoint("mailpit", "http") };
        var query = Uri.EscapeDataString($"subject:\"{ticketSubject}\"");
        var found = await mailpit.GetFromJsonAsync<JsonObject>(new Uri($"/api/v1/search?query={query}", UriKind.Relative), cancellationToken);
        return [.. found!["messages"]!.AsArray().Select(message => message!.AsObject())];
    }
}
