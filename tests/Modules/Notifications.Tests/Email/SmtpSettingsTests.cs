using FabricPilot.Modules.Notifications.Infrastructure.Email;
using MailKit.Security;
using Microsoft.Extensions.Configuration;

namespace FabricPilot.Modules.Notifications.Tests.Email;

public class SmtpSettingsTests
{
    [Theory]
    [InlineData("smtp://relay.example.com:587", "relay.example.com", 587, SecureSocketOptions.StartTls)]
    [InlineData("smtps://relay.example.com:465", "relay.example.com", 465, SecureSocketOptions.SslOnConnect)]
    public void A_mail_server_is_reached_over_tls_whatever_it_advertises(string connection, string host, int port, SecureSocketOptions security)
    {
        var settings = SmtpSettings.From(Configuration(connection));

        settings.ShouldBe(new SmtpSettings(host, port, "noreply@example.com", security));
    }

    [Fact]
    public void Only_a_configuration_that_says_so_sends_unencrypted()
    {
        var settings = SmtpSettings.From(Configuration("smtp://localhost:1025", ("Notifications:AllowUnencryptedSmtp", "true")));

        settings.Security.ShouldBe(SecureSocketOptions.None);
    }

    [Theory]
    [InlineData("http://relay.example.com:25")]
    [InlineData("relay.example.com")]
    public void A_connection_string_that_names_no_smtp_server_is_refused(string connection)
    {
        Should.Throw<InvalidOperationException>(() => SmtpSettings.From(Configuration(connection)));
    }

    [Fact]
    public void Without_a_mail_server_nothing_is_sent()
    {
        Should.Throw<InvalidOperationException>(() => SmtpSettings.From(new ConfigurationBuilder().Build()));
    }

    [Fact]
    public void A_user_name_and_a_password_sign_in_to_the_server()
    {
        var settings = SmtpSettings.From(Configuration(
            "smtps://relay.example.com:465",
            ("Notifications:SmtpUserName", "portal"),
            ("Notifications:SmtpPassword", "correct-horse")));

        settings.Credentials.ShouldBe(new SmtpCredentials("portal", "correct-horse"));
    }

    [Fact]
    public void Without_a_user_name_and_a_password_the_server_is_not_signed_in_to()
    {
        var settings = SmtpSettings.From(Configuration("smtps://relay.example.com:465"));

        settings.Credentials.ShouldBeNull();
    }

    [Theory]
    [InlineData("Notifications:SmtpUserName", "portal")]
    [InlineData("Notifications:SmtpPassword", "correct-horse")]
    public void A_user_name_without_a_password_or_a_password_without_a_user_name_is_refused(string key, string value)
    {
        Should.Throw<InvalidOperationException>(() => SmtpSettings.From(Configuration("smtps://relay.example.com:465", (key, value))));
    }

    [Fact]
    public void The_settings_name_the_user_but_never_show_the_password()
    {
        var settings = SmtpSettings.From(Configuration(
            "smtps://relay.example.com:465",
            ("Notifications:SmtpUserName", "portal"),
            ("Notifications:SmtpPassword", "correct-horse")));

        var text = settings.ToString();

        text.ShouldContain("portal");
        text.ShouldNotContain("correct-horse");
    }

    private static IConfiguration Configuration(string connection, params (string Key, string Value)[] more)
    {
        Dictionary<string, string?> values = new()
        {
            ["ConnectionStrings:mail"] = connection,
            ["Notifications:SenderAddress"] = "noreply@example.com",
        };
        foreach (var (key, value) in more)
        {
            values[key] = value;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }
}
