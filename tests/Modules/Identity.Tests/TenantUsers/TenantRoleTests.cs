using FabricPilot.Modules.Identity.Domain.TenantUsers;

namespace FabricPilot.Modules.Identity.Tests.TenantUsers;

public class TenantRoleTests
{
    [Theory]
    [InlineData("customer-user")]
    [InlineData("customer-admin")]
    public void A_role_on_the_allow_list_is_grantable(string name)
    {
        var role = TenantRole.From(name);

        role.IsSuccess.ShouldBeTrue();
        role.Value.Name.ShouldBe(name);
    }

    [Fact]
    public void The_allow_list_is_the_customer_user_and_the_customer_admin_role()
    {
        TenantRole.All.Select(role => role.Name).ShouldBe(["customer-user", "customer-admin"]);
        TenantRole.CustomerUser.Name.ShouldBe("customer-user");
        TenantRole.CustomerAdmin.Name.ShouldBe("customer-admin");
    }

    [Theory]
    [InlineData("staff")]
    [InlineData("manager")]
    [InlineData("Customer-User")]
    [InlineData("CUSTOMER-ADMIN")]
    [InlineData(" customer-user")]
    [InlineData("")]
    [InlineData("offline_access")]
    [InlineData("default-roles-fabric-pilot")]
    [InlineData("realm-admin")]
    [InlineData("manage-users")]
    public void Every_other_role_is_refused(string name)
    {
        var role = TenantRole.From(name);

        role.IsFailure.ShouldBeTrue();
        var error = role.Error.ShouldBeOfType<RoleNotGrantable>();
        error.Code.ShouldBe("identity.role-not-grantable");
        error.Role.ShouldBe(name);
    }
}
