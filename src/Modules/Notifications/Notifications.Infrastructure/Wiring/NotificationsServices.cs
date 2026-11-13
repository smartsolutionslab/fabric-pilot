using FabricPilot.Modules.Notifications.Application;
using FabricPilot.Modules.Notifications.Infrastructure;
using FabricPilot.Modules.Notifications.Infrastructure.Email;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class NotificationsServices
{
    /// <summary>The notices table, and e-mail through the SMTP server of the connection string <c>mail</c>.</summary>
    public static IServiceCollection AddNotificationsServices(this IServiceCollection services, IConfiguration configuration)
    {
        Ensure.That(configuration).IsNotNull();

        return services
            .AddScoped<INoticeStore, NoticeStore>()
            .AddSingleton(_ => SmtpSettings.From(configuration))
            .AddSingleton<IEmailSender, MailKitEmailSender>();
    }
}
