using FabricPilot.Modules.Notifications.Application;

namespace FabricPilot.Modules.Notifications.Tests;

/// <summary>Keeps every e-mail it is given; fails the next send when told to, as a mail server that is down would.</summary>
internal sealed class RecordingEmailSender : IEmailSender
{
    public List<EmailMessage> Sent { get; } = [];

    public bool FailsNextSend { get; set; }

    /// <summary>Makes every send wait until it is cancelled, as a mail server that keeps answering slowly would.</summary>
    public bool Hangs { get; set; }

    /// <summary>Completes when a send has started.</summary>
    public TaskCompletionSource SendStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Runs once, in the middle of the next send: another delivery arriving meanwhile.</summary>
    public Func<Task>? WhileSending { get; set; }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        if (FailsNextSend)
        {
            FailsNextSend = false;
            throw new IOException("The mail server is unreachable.");
        }

        SendStarted.TrySetResult();
        if (Hangs)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }

        if (WhileSending is { } meanwhile)
        {
            WhileSending = null;
            await meanwhile();
        }

        Sent.Add(message);
    }
}
