namespace FabricPilot.Modules.Notifications.Infrastructure.Email;

/// <summary>
/// Where e-mail goes and whom it is from: the connection string <c>mail</c> (<c>smtp://host:port</c>, Mailpit's in
/// development, from the AppHost) and <c>Notifications:SenderAddress</c>.
/// </summary>
public sealed record SmtpSettings(string Host, int Port, string SenderAddress);
