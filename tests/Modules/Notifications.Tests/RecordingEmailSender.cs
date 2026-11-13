using FabricPilot.Modules.Notifications.Application;

namespace FabricPilot.Modules.Notifications.Tests;

/// <summary>Keeps every e-mail it is given; fails the next send when told to, as a mail server that is down would.</summary>
internal sealed class RecordingEmailSender : IEmailSender
{
    public List<EmailMessage> Sent { get; } = [];

    public bool FailsNextSend { get; set; }

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        if (FailsNextSend)
        {
            FailsNextSend = false;
            throw new IOException("The mail server is unreachable.");
        }

        Sent.Add(message);
        return Task.CompletedTask;
    }
}
