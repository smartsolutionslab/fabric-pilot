using System.Net.Http.Json;
using Aspire.Hosting.Testing;

namespace FabricPilot.Integration.Tests;

/// <summary>The local mail catcher's inbox, read through its API.</summary>
public sealed class MailpitInbox(AspireFixture aspire)
{
    /// <summary>How many messages Mailpit caught for the address, waiting up to 30 seconds for at least <paramref name="expected"/>.</summary>
    public async Task<int> CountForAsync(string address, int expected, CancellationToken cancellationToken)
    {
        using var mailpit = new HttpClient { BaseAddress = aspire.App.GetEndpoint("mailpit", "http") };
        var count = 0;
        for (var attempt = 0; attempt < 30; attempt++)
        {
            var inbox = await mailpit.GetFromJsonAsync<Inbox>(new Uri($"/api/v1/search?query=to:{Uri.EscapeDataString(address)}", UriKind.Relative), cancellationToken);
            count = inbox?.Messages.Count(message => message.To.Any(to => string.Equals(to.Address, address, StringComparison.OrdinalIgnoreCase))) ?? 0;
            if (count >= expected)
            {
                return count;
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        return count;
    }

    private sealed record Inbox(IReadOnlyList<Message> Messages);

    private sealed record Message(IReadOnlyList<Recipient> To);

    private sealed record Recipient(string Address);
}
