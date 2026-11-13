using System.Net;
using FabricPilot.Modules.Identity.Application;
using FabricPilot.Modules.Identity.Application.Commands.InviteTenantUser;
using FabricPilot.Modules.Identity.Domain.TenantUsers;
using Microsoft.Extensions.Logging.Abstractions;

namespace FabricPilot.Modules.Identity.Tests.Commands;

public class InviteTenantUserTests
{
    private readonly ITenantUserDirectory directory = Substitute.For<ITenantUserDirectory>();
    private readonly TenantUserId created = DirectoryUsers.NewId();

    public InviteTenantUserTests()
    {
        directory.CreateAsync(Arg.Any<TenantGroup>(), Arg.Any<NewTenantUser>(), Arg.Any<CancellationToken>()).Returns(Option<TenantUserId>.Some(created));
        directory.SendInvitationAsync(Arg.Any<TenantUserId>(), Arg.Any<CancellationToken>()).Returns(true);
    }

    [Fact]
    public async Task An_invited_user_is_created_in_the_callers_tenant_group_given_the_role_and_sent_the_invitation()
    {
        var result = await Handle("tenant-a", new InviteTenantUser("  New.User@Example.com ", "New", "User", "customer-admin", Guid.CreateVersion7()));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(created.Value);
        await directory.Received(1).CreateAsync(
            Arg.Is<TenantGroup>(group => group.Path == "/tenants/tenant-a"),
            Arg.Is<NewTenantUser>(user => user.Email == "new.user@example.com" && user.FirstName == "New" && user.LastName == "User"),
            Arg.Any<CancellationToken>());
        await directory.Received(1).SetRoleAsync(created, TenantRole.CustomerAdmin, Arg.Any<CancellationToken>());
        await directory.Received(1).SendInvitationAsync(created, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Without_a_tenant_the_caller_is_refused_and_nothing_reaches_the_directory()
    {
        var result = await Handle(null, new InviteTenantUser("new.user@example.com", "New", "User", "customer-user", Guid.CreateVersion7()));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("identity.no-tenant");
        result.Error.Status.ShouldBe(HttpStatusCode.Forbidden);
        directory.ReceivedCalls().ShouldBeEmpty();
    }

    [Theory]
    [InlineData("staff")]
    [InlineData("manager")]
    [InlineData("Customer-Admin")]
    [InlineData("default-roles-fabric-pilot")]
    [InlineData("manage-users")]
    [InlineData("")]
    public async Task A_role_outside_the_allow_list_is_refused_before_the_directory_is_called(string role)
    {
        var result = await Handle("tenant-a", new InviteTenantUser("new.user@example.com", "New", "User", role, Guid.CreateVersion7()));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("identity.role-not-grantable");
        result.Error.Status.ShouldBe(HttpStatusCode.UnprocessableEntity);
        directory.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task An_address_that_is_not_an_email_is_refused_before_the_directory_is_called()
    {
        var result = await Handle("tenant-a", new InviteTenantUser("no-at-sign", "New", "User", "customer-user", Guid.CreateVersion7()));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("identity.invalid-email");
        result.Error.Status.ShouldBe(HttpStatusCode.UnprocessableEntity);
        directory.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task A_user_without_a_name_is_refused_before_the_directory_is_called()
    {
        var result = await Handle("tenant-a", new InviteTenantUser("new.user@example.com", " ", "User", "customer-user", Guid.CreateVersion7()));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("identity.invalid-name");
        result.Error.Status.ShouldBe(HttpStatusCode.UnprocessableEntity);
        directory.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task An_address_the_realm_already_has_is_a_conflict_and_no_role_is_granted()
    {
        directory.CreateAsync(Arg.Any<TenantGroup>(), Arg.Any<NewTenantUser>(), Arg.Any<CancellationToken>()).Returns(Option<TenantUserId>.None);

        var result = await Handle("tenant-a", new InviteTenantUser("taken@example.com", "New", "User", "customer-user", Guid.CreateVersion7()));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("identity.user-already-exists");
        result.Error.Status.ShouldBe(HttpStatusCode.Conflict);
        await directory.DidNotReceiveWithAnyArgs().SetRoleAsync(default!, default!, default);
        await directory.DidNotReceiveWithAnyArgs().SendInvitationAsync(default!, default);
    }

    [Fact]
    public async Task When_the_role_cannot_be_granted_the_new_user_is_deleted_and_the_failure_rethrown()
    {
        directory.SetRoleAsync(created, Arg.Any<TenantRole>(), Arg.Any<CancellationToken>()).Returns<Task>(_ => throw new HttpRequestException("Keycloak answered 503."));

        var handle = () => Handle("tenant-a", new InviteTenantUser("new.user@example.com", "New", "User", "customer-user", Guid.CreateVersion7()));

        (await Should.ThrowAsync<HttpRequestException>(handle)).Message.ShouldBe("Keycloak answered 503.");
        await directory.Received(1).DeleteAsync(created, Arg.Any<CancellationToken>());
        await directory.DidNotReceiveWithAnyArgs().SendInvitationAsync(default!, default);
    }

    [Fact]
    public async Task An_invitation_email_that_could_not_be_sent_still_leaves_the_user_invited_with_the_role()
    {
        directory.SendInvitationAsync(created, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handle("tenant-a", new InviteTenantUser("new.user@example.com", "New", "User", "customer-user", Guid.CreateVersion7()));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(created.Value);
        await directory.Received(1).SetRoleAsync(created, TenantRole.CustomerUser, Arg.Any<CancellationToken>());
        await directory.DidNotReceiveWithAnyArgs().DeleteAsync(default!, default);
    }

    private Task<Result<Guid, ApiError>> Handle(string? tenantId, InviteTenantUser command) =>
        new InviteTenantUserHandler(DirectoryUsers.Tenant(tenantId), directory, NullLogger<InviteTenantUserHandler>.Instance)
            .HandleAsync(command, TestContext.Current.CancellationToken);
}
