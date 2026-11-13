using FabricPilot.Modules.Notifications.Application;
using FabricPilot.Modules.Notifications.Infrastructure;
using FabricPilot.Modules.Notifications.Infrastructure.Email;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class NotificationsServices
{
    private const string MailConnectionName = "mail";

    private const string SenderAddressKey = "Notifications:SenderAddress";

    /// <summary>The notices table, and e-mail through the SMTP server of the connection string <c>mail</c>.</summary>
    public static IServiceCollection AddNotificationsServices(this IServiceCollection services, IConfiguration configuration)
    {
        Ensure.That(configuration).IsNotNull();

        return services
            .AddScoped<INoticeStore, NoticeStore>()
            .AddSingleton(_ => Settings(configuration))
            .AddSingleton<IEmailSender, MailKitEmailSender>();
    }

    private static SmtpSettings Settings(IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString(MailConnectionName)
            ?? throw new InvalidOperationException($"Connection string '{MailConnectionName}' is missing; the AppHost provides Mailpit's in development.");
        var sender = configuration[SenderAddressKey]
            ?? throw new InvalidOperationException($"{SenderAddressKey} is missing; the AppHost provides it in development.");
        var server = new Uri(connection);
        return new SmtpSettings(server.Host, server.Port, sender);
    }
}
