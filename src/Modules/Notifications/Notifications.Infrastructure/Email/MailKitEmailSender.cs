using FabricPilot.Modules.Notifications.Application;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace FabricPilot.Modules.Notifications.Infrastructure.Email;

/// <summary>
/// Sends each e-mail over its own SMTP connection, encrypted as <see cref="SmtpSettings"/> says. E-mail is hand-written
/// in this repository until the catalog offers it.
/// </summary>
public sealed class MailKitEmailSender(SmtpSettings settings) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        Ensure.That(message).IsNotNull();

        using var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(settings.SenderAddress));
        email.To.Add(MailboxAddress.Parse(message.To));
        email.Subject = message.Subject;
        email.Body = new TextPart("plain") { Text = message.Body };

        // Shorter than a claim's lease, so a send that hangs fails before another delivery may send the same e-mail.
        using var client = new SmtpClient { Timeout = (int)TimeSpan.FromSeconds(30).TotalMilliseconds };
        await client.ConnectAsync(settings.Host, settings.Port, settings.Security, cancellationToken);
        await client.SendAsync(email, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);
    }
}
