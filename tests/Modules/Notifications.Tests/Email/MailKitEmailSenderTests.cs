using FabricPilot.Modules.Notifications.Application;
using FabricPilot.Modules.Notifications.Infrastructure.Email;
using MailKit.Security;

namespace FabricPilot.Modules.Notifications.Tests.Email;

/// <summary>
/// What the sender does against a server that never accepts the message or offers no TLS. Sending through a real server,
/// signed in or not, is in the integration tests against Mailpit.
/// </summary>
public class MailKitEmailSenderTests
{
    private static readonly EmailMessage Message = new("customer@example.com", "Ticket updated", "Your ticket was resolved.");

    [Fact]
    public async Task A_send_the_caller_cancels_stops_before_the_server_has_accepted_the_message()
    {
        await using var server = new StallingSmtpServer();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var sending = Sender(server.Port, SecureSocketOptions.None).SendAsync(Message, cancellation.Token);

        await server.DataReceived.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => sending.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_server_that_offers_no_starttls_is_refused_when_tls_is_required()
    {
        await using var server = new StallingSmtpServer();

        await Should.ThrowAsync<NotSupportedException>(() => Sender(server.Port, SecureSocketOptions.StartTls)
            .SendAsync(Message, TestContext.Current.CancellationToken));

        server.DataReceived.IsCompleted.ShouldBeFalse();
    }

    [Fact]
    public async Task A_server_that_does_not_speak_tls_is_refused_when_tls_comes_first()
    {
        await using var server = new StallingSmtpServer();

        await Should.ThrowAsync<SslHandshakeException>(() => Sender(server.Port, SecureSocketOptions.SslOnConnect)
            .SendAsync(Message, TestContext.Current.CancellationToken));

        server.DataReceived.IsCompleted.ShouldBeFalse();
    }

    private static MailKitEmailSender Sender(int port, SecureSocketOptions security) =>
        new(new SmtpSettings("127.0.0.1", port, "noreply@example.com", security));
}
