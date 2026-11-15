using FabricPilot.Modules.Identity.Domain.TenantUsers;

namespace FabricPilot.Modules.Identity.Tests.TenantUsers;

public class TenantGroupTests
{
    [Fact]
    public void A_tenants_group_is_its_path_under_tenants()
    {
        var group = TenantGroup.For("tenant-a");

        group.HasValue.ShouldBeTrue();
        group.Value.TenantId.ShouldBe("tenant-a");
        group.Value.Path.ShouldBe("/tenants/tenant-a");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("tenant-a/../tenant-b")]
    [InlineData("tenant-a/sub")]
    [InlineData("/tenant-a")]
    [InlineData("tenant-a\\tenant-b")]
    public void A_tenant_id_that_could_name_another_group_has_none(string tenantId)
    {
        TenantGroup.For(tenantId).HasValue.ShouldBeFalse();
    }
}
