namespace FabricPilot.Modules.Notifications.Application;

/// <summary>Sends e-mail; implemented in the module's Infrastructure project. A failure throws, so the caller's message is retried.</summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
