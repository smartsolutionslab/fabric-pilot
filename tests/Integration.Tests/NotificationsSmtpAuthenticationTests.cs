using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Aspire.Hosting.Testing;
using FabricPilot.Modules.Notifications.Application;
using FabricPilot.Modules.Notifications.Infrastructure.Email;
using MailKit.Security;
using Microsoft.Extensions.Configuration;

namespace FabricPilot.Integration.Tests;

/// <summary>
/// The notifications module's e-mail through an SMTP server that takes mail only from a signed-in client, as Azure
/// Communication Services' and most providers' do: the fixture's authenticating Mailpit. The sender is configured as the
/// host is, through <see cref="SmtpSettings.From"/>. A send that fails throws, and the consumer gives its claim back and
/// throws too, so the event is retried and then dead-lettered like any other failed delivery.
/// </summary>
public class NotificationsSmtpAuthenticationTests(AspireFixture aspire)
{
    [Fact]
    public async Task A_server_that_requires_authentication_takes_the_email_of_the_configured_user()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var subject = $"Signed in {Guid.CreateVersion7()}";

        await Sender(AspireFixture.SmtpUserName, AspireFixture.SmtpPassword)
            .SendAsync(new EmailMessage("customer@example.com", subject, "Sent by a signed-in client."), cancellationToken);

        (await CaughtAsync(subject, cancellationToken)).ShouldBe(1);
    }

    [Fact]
    public async Task A_wrong_password_fails_the_send_and_delivers_nothing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var subject = $"Wrong password {Guid.CreateVersion7()}";

        await Should.ThrowAsync<AuthenticationException>(() => Sender(AspireFixture.SmtpUserName, "wrong-password")
            .SendAsync(new EmailMessage("customer@example.com", subject, "Never delivered."), cancellationToken));

        (await CaughtAsync(subject, cancellationToken)).ShouldBe(0);
    }

    private MailKitEmailSender Sender(string userName, string password)
    {
        var smtp = aspire.App.GetEndpoint(AspireFixture.AuthenticatingSmtp, "smtp");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:mail"] = $"smtp://{smtp.Host}:{smtp.Port}",
                ["Notifications:SenderAddress"] = "noreply@example.com",
                ["Notifications:AllowUnencryptedSmtp"] = "true",
                ["Notifications:SmtpUserName"] = userName,
                ["Notifications:SmtpPassword"] = password,
            })
            .Build();
        return new MailKitEmailSender(SmtpSettings.From(configuration));
    }

    /// <summary>How many messages the authenticating Mailpit caught with this subject, through its API.</summary>
    private async Task<int> CaughtAsync(string subject, CancellationToken cancellationToken)
    {
        using var mailpit = new HttpClient { BaseAddress = aspire.App.GetEndpoint(AspireFixture.AuthenticatingSmtp, "http") };
        var query = Uri.EscapeDataString($"subject:\"{subject}\"");
        var found = await mailpit.GetFromJsonAsync<JsonObject>(new Uri($"/api/v1/search?query={query}", UriKind.Relative), cancellationToken);
        return found!["messages"]!.AsArray().Count;
    }
}
