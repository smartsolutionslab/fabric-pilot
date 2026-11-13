using FabricPilot.Modules.Identity.Domain.TenantUsers;

namespace FabricPilot.Modules.Identity.Tests.TenantUsers;

public class TenantUserTests
{
    [Fact]
    public void A_users_tenant_roles_are_the_allow_listed_roles_they_hold()
    {
        var user = new TenantUser(new TenantUserId(Guid.CreateVersion7()), "a@example.com", "A", "User", ["/tenants/tenant-a"], ["manager", "customer-admin", "customer-user", "offline_access"]);

        user.TenantRoles.ShouldBe([TenantRole.CustomerUser, TenantRole.CustomerAdmin]);
        user.Holds(TenantRole.CustomerAdmin).ShouldBeTrue();
    }

    [Fact]
    public void A_user_without_an_allow_listed_role_has_no_tenant_role()
    {
        var user = new TenantUser(new TenantUserId(Guid.CreateVersion7()), "a@example.com", "A", "User", ["/tenants/tenant-a"], ["manager"]);

        user.TenantRoles.ShouldBeEmpty();
        user.Holds(TenantRole.CustomerUser).ShouldBeFalse();
    }
}
