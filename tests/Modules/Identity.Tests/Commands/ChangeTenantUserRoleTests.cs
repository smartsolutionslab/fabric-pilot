using System.Net;
using FabricPilot.Modules.Identity.Application;
using FabricPilot.Modules.Identity.Application.Commands.ChangeTenantUserRole;
using FabricPilot.Modules.Identity.Domain.TenantUsers;
using Microsoft.Extensions.Logging.Abstractions;

namespace FabricPilot.Modules.Identity.Tests.Commands;

public class ChangeTenantUserRoleTests
{
    private static readonly string[] TenantA = ["/tenants/tenant-a"];

    private readonly Guid caller = Guid.CreateVersion7();

    [Fact]
    public async Task A_user_of_the_callers_tenant_gets_the_new_role()
    {
        var user = DirectoryUsers.User(DirectoryUsers.NewId(), TenantA, "customer-user");
        var directory = DirectoryUsers.Knowing(user);

        var result = await Handle(directory, "tenant-a", new ChangeTenantUserRole(user.Id.Value, "customer-admin", caller));

        result.IsSuccess.ShouldBeTrue();
        await directory.Received(1).SetRoleAsync(user.Id, TenantRole.CustomerAdmin, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData]
    [InlineData("/tenants/tenant-b")]
    [InlineData("/tenants/tenant-a", "/tenants/tenant-b")]
    [InlineData("/tenants/tenant-a/sub")]
    [InlineData("/staff")]
    public async Task A_user_who_is_not_only_in_the_callers_tenant_is_not_found(params string[] groups)
    {
        var user = DirectoryUsers.User(DirectoryUsers.NewId(), groups, "customer-user");
        var directory = DirectoryUsers.Knowing(user);

        var result = await Handle(directory, "tenant-a", new ChangeTenantUserRole(user.Id.Value, "customer-admin", caller));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("identity.user-not-found");
        result.Error.Status.ShouldBe(HttpStatusCode.NotFound);
        await directory.DidNotReceiveWithAnyArgs().SetRoleAsync(default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task A_staff_member_in_the_callers_group_is_not_found_either()
    {
        var user = DirectoryUsers.User(DirectoryUsers.NewId(), TenantA, "staff");
        var directory = DirectoryUsers.Knowing(user);

        var result = await Handle(directory, "tenant-a", new ChangeTenantUserRole(user.Id.Value, "customer-user", caller));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("identity.user-not-found");
        await directory.DidNotReceiveWithAnyArgs().SetRoleAsync(default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task A_user_the_realm_does_not_have_is_not_found()
    {
        var directory = DirectoryUsers.Knowing();

        var result = await Handle(directory, "tenant-a", new ChangeTenantUserRole(Guid.CreateVersion7(), "customer-user", caller));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("identity.user-not-found");
        result.Error.Status.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Nobody_changes_their_own_role()
    {
        var self = DirectoryUsers.User(new TenantUserId(caller), TenantA, "customer-admin");
        var directory = DirectoryUsers.Knowing(self);

        var result = await Handle(directory, "tenant-a", new ChangeTenantUserRole(caller, "customer-user", caller));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("identity.cannot-change-own-role");
        result.Error.Status.ShouldBe(HttpStatusCode.Conflict);
        directory.ReceivedCalls().ShouldBeEmpty();
    }

    [Theory]
    [InlineData("staff")]
    [InlineData("manager")]
    [InlineData("offline_access")]
    public async Task A_role_outside_the_allow_list_is_refused_before_the_directory_is_called(string role)
    {
        var directory = DirectoryUsers.Knowing();

        var result = await Handle(directory, "tenant-a", new ChangeTenantUserRole(Guid.CreateVersion7(), role, caller));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("identity.role-not-grantable");
        result.Error.Status.ShouldBe(HttpStatusCode.UnprocessableEntity);
        directory.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task Without_a_tenant_the_caller_is_refused_and_nothing_reaches_the_directory()
    {
        var directory = DirectoryUsers.Knowing();

        var result = await Handle(directory, null, new ChangeTenantUserRole(Guid.CreateVersion7(), "customer-user", caller));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("identity.no-tenant");
        result.Error.Status.ShouldBe(HttpStatusCode.Forbidden);
        directory.ReceivedCalls().ShouldBeEmpty();
    }

    private static Task<Result<Unit, ApiError>> Handle(ITenantUserDirectory directory, string? tenantId, ChangeTenantUserRole command) =>
        new ChangeTenantUserRoleHandler(DirectoryUsers.Tenant(tenantId), directory, NullLogger<ChangeTenantUserRoleHandler>.Instance)
            .HandleAsync(command, TestContext.Current.CancellationToken);
}
