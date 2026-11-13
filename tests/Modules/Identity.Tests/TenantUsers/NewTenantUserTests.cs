using FabricPilot.Modules.Identity.Domain.TenantUsers;

namespace FabricPilot.Modules.Identity.Tests.TenantUsers;

public class NewTenantUserTests
{
    [Fact]
    public void The_email_is_trimmed_and_lower_cased_and_the_names_trimmed()
    {
        var user = NewTenantUser.Create("  New.User@Example.COM ", " New ", " User ");

        user.IsSuccess.ShouldBeTrue();
        user.Value.Email.ShouldBe("new.user@example.com");
        user.Value.FirstName.ShouldBe("New");
        user.Value.LastName.ShouldBe("User");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("no-at-sign")]
    [InlineData("@example.com")]
    [InlineData("someone@")]
    [InlineData("two@at@example.com")]
    [InlineData("a b@example.com")]
    public void An_address_that_is_not_an_email_is_refused(string email)
    {
        var user = NewTenantUser.Create(email, "New", "User");

        user.IsFailure.ShouldBeTrue();
        user.Error.ShouldBeOfType<InvalidEmail>().Code.ShouldBe("identity.invalid-email");
    }

    [Fact]
    public void An_address_longer_than_the_realm_keeps_is_refused()
    {
        var email = new string('a', NewTenantUser.MaximumLength - "@example.com".Length + 1) + "@example.com";

        var user = NewTenantUser.Create(email, "New", "User");

        user.IsFailure.ShouldBeTrue();
        user.Error.ShouldBeOfType<InvalidEmail>();
    }

    [Theory]
    [InlineData("", "User")]
    [InlineData(" ", "User")]
    [InlineData("New", "")]
    [InlineData("New", " ")]
    public void A_user_needs_a_first_and_a_last_name(string firstName, string lastName)
    {
        var user = NewTenantUser.Create("new.user@example.com", firstName, lastName);

        user.IsFailure.ShouldBeTrue();
        var error = user.Error.ShouldBeOfType<InvalidName>();
        error.Code.ShouldBe("identity.invalid-name");
        error.Maximum.ShouldBe(NewTenantUser.MaximumLength);
    }

    [Fact]
    public void A_name_longer_than_the_realm_keeps_is_refused()
    {
        var user = NewTenantUser.Create("new.user@example.com", new string('a', NewTenantUser.MaximumLength + 1), "User");

        user.IsFailure.ShouldBeTrue();
        user.Error.ShouldBeOfType<InvalidName>();
    }
}
