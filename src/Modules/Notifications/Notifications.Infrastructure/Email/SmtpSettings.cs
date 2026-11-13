using MailKit.Security;
using Microsoft.Extensions.Configuration;

namespace FabricPilot.Modules.Notifications.Infrastructure.Email;

/// <summary>
/// Where e-mail goes, whom it is from, and how it is encrypted: the connection string <c>mail</c>
/// (<c>smtp://host:port</c> for STARTTLS, <c>smtps://host:port</c> for TLS from the start) and
/// <c>Notifications:SenderAddress</c>. A server that does not offer TLS is refused, unless
/// <c>Notifications:AllowUnencryptedSmtp</c> is true, which only the AppHost sets, for the local Mailpit.
/// </summary>
public sealed record SmtpSettings(string Host, int Port, string SenderAddress, SecureSocketOptions Security)
{
    private const string MailConnectionName = "mail";

    private const string SenderAddressKey = "Notifications:SenderAddress";

    private const string AllowUnencryptedKey = "Notifications:AllowUnencryptedSmtp";

    public static SmtpSettings From(IConfiguration configuration)
    {
        Ensure.That(configuration).IsNotNull();

        var connection = configuration.GetConnectionString(MailConnectionName)
            ?? throw new InvalidOperationException($"Connection string '{MailConnectionName}' is missing; the AppHost provides Mailpit's in development.");
        var sender = configuration[SenderAddressKey]
            ?? throw new InvalidOperationException($"{SenderAddressKey} is missing; the AppHost provides it in development.");
        if (!Uri.TryCreate(connection, UriKind.Absolute, out var server) || server.Scheme is not ("smtp" or "smtps"))
        {
            throw new InvalidOperationException($"Connection string '{MailConnectionName}' must be smtp://host:port or smtps://host:port.");
        }

        var security = server.Scheme == "smtps" ? SecureSocketOptions.SslOnConnect
            : configuration.GetValue<bool>(AllowUnencryptedKey) ? SecureSocketOptions.None
            : SecureSocketOptions.StartTls;
        return new SmtpSettings(server.Host, server.Port, sender, security);
    }
}
