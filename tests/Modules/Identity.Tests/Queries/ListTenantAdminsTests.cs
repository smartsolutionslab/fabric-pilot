using System.Net;
using FabricPilot.Modules.Identity.Application;
using FabricPilot.Modules.Identity.Application.DTOs;
using FabricPilot.Modules.Identity.Application.Queries.ListTenantAdmins;
using FabricPilot.Modules.Identity.Domain.TenantUsers;

namespace FabricPilot.Modules.Identity.Tests.Queries;

public class ListTenantAdminsTests
{
    private static readonly string[] TenantB = ["/tenants/tenant-b"];

    [Fact]
    public async Task Staff_list_the_members_of_the_named_tenant_who_are_its_admins()
    {
        var admin = DirectoryUsers.User(DirectoryUsers.NewId(), TenantB, "customer-admin", "customer-user");
        var user = DirectoryUsers.User(DirectoryUsers.NewId(), TenantB, "customer-user");
        var otherAdmin = DirectoryUsers.User(DirectoryUsers.NewId(), TenantB, "customer-admin");
        var directory = DirectoryUsers.Knowing(admin, user, otherAdmin);
        directory.TenantExistsAsync(Arg.Is<TenantGroup>(group => group.Path == "/tenants/tenant-b"), Arg.Any<CancellationToken>()).Returns(true);
        directory.ListMembersAsync(Arg.Is<TenantGroup>(group => group.Path == "/tenants/tenant-b"), Arg.Any<CancellationToken>())
            .Returns([admin.Id, user.Id, otherAdmin.Id]);

        var result = await Handle(directory, new ListTenantAdmins("tenant-b", Page.First, PageSize.Default));

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(2);
        result.Value.Items.Select(item => item.Id).ShouldBe([admin.Id.Value, otherAdmin.Id.Value]);
    }

    [Theory]
    [InlineData("tenant-c")]
    [InlineData("tenant-b/..")]
    public async Task A_tenant_the_realm_does_not_have_is_not_found(string tenantId)
    {
        var directory = DirectoryUsers.Knowing();

        var result = await Handle(directory, new ListTenantAdmins(tenantId, Page.First, PageSize.Default));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("identity.tenant-not-found");
        result.Error.Status.ShouldBe(HttpStatusCode.NotFound);
        await directory.DidNotReceiveWithAnyArgs().ListMembersAsync(default!, TestContext.Current.CancellationToken);
    }

    private static Task<Result<PagedResult<TenantUserDto>, ApiError>> Handle(ITenantUserDirectory directory, ListTenantAdmins query) =>
        new ListTenantAdminsHandler(directory).HandleAsync(query, TestContext.Current.CancellationToken);
}
