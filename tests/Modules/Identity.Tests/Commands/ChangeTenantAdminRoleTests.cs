using System.Net;
using FabricPilot.Modules.Identity.Application;
using FabricPilot.Modules.Identity.Application.Commands.ChangeTenantAdminRole;
using FabricPilot.Modules.Identity.Domain.TenantUsers;
using Microsoft.Extensions.Logging.Abstractions;

namespace FabricPilot.Modules.Identity.Tests.Commands;

public class ChangeTenantAdminRoleTests
{
    private static readonly string[] TenantA = ["/tenants/tenant-a"];
    private static readonly string[] TenantB = ["/tenants/tenant-b"];

    [Fact]
    public async Task Staff_demote_an_admin_of_the_tenant_the_route_names()
    {
        var admin = DirectoryUsers.User(DirectoryUsers.NewId(), TenantB, "customer-admin", "customer-user");
        var directory = Existing(admin);

        var result = await Handle(directory, new ChangeTenantAdminRole("tenant-b", admin.Id.Value, "customer-user", Guid.CreateVersion7()));

        result.IsSuccess.ShouldBeTrue();
        await directory.Received(1).SetRoleAsync(admin.Id, TenantRole.CustomerUser, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("staff")]
    [InlineData("manager")]
    public async Task Staff_grant_no_role_outside_the_allow_list_either(string role)
    {
        var admin = DirectoryUsers.User(DirectoryUsers.NewId(), TenantB, "customer-admin");
        var directory = Existing(admin);

        var result = await Handle(directory, new ChangeTenantAdminRole("tenant-b", admin.Id.Value, role, Guid.CreateVersion7()));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("identity.role-not-grantable");
        result.Error.Status.ShouldBe(HttpStatusCode.UnprocessableEntity);
        await directory.DidNotReceiveWithAnyArgs().SetRoleAsync(default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task A_user_of_another_tenant_than_the_route_names_is_not_found()
    {
        var user = DirectoryUsers.User(DirectoryUsers.NewId(), TenantA, "customer-admin");
        var directory = Existing(user);

        var result = await Handle(directory, new ChangeTenantAdminRole("tenant-b", user.Id.Value, "customer-user", Guid.CreateVersion7()));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("identity.user-not-found");
        await directory.DidNotReceiveWithAnyArgs().SetRoleAsync(default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task A_tenant_the_realm_does_not_have_is_not_found()
    {
        var directory = Existing();

        var result = await Handle(directory, new ChangeTenantAdminRole("tenant-c", Guid.CreateVersion7(), "customer-user", Guid.CreateVersion7()));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("identity.tenant-not-found");
        result.Error.Status.ShouldBe(HttpStatusCode.NotFound);
        await directory.DidNotReceiveWithAnyArgs().SetRoleAsync(default!, default!, TestContext.Current.CancellationToken);
    }

    private static ITenantUserDirectory Existing(params TenantUser[] users)
    {
        var directory = DirectoryUsers.Knowing(users);
        directory.TenantExistsAsync(Arg.Is<TenantGroup>(group => group.Path == "/tenants/tenant-b"), Arg.Any<CancellationToken>()).Returns(true);
        return directory;
    }

    private static Task<Result<Unit, ApiError>> Handle(ITenantUserDirectory directory, ChangeTenantAdminRole command) =>
        new ChangeTenantAdminRoleHandler(directory, NullLogger<ChangeTenantAdminRoleHandler>.Instance)
            .HandleAsync(command, TestContext.Current.CancellationToken);
}
