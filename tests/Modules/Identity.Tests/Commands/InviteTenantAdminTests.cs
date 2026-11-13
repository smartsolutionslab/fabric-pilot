using System.Net;
using FabricPilot.Modules.Identity.Application;
using FabricPilot.Modules.Identity.Application.Commands.InviteTenantAdmin;
using FabricPilot.Modules.Identity.Domain.TenantUsers;
using Microsoft.Extensions.Logging.Abstractions;

namespace FabricPilot.Modules.Identity.Tests.Commands;

public class InviteTenantAdminTests
{
    private readonly ITenantUserDirectory directory = Substitute.For<ITenantUserDirectory>();
    private readonly TenantUserId created = DirectoryUsers.NewId();

    public InviteTenantAdminTests()
    {
        directory.TenantExistsAsync(Arg.Is<TenantGroup>(group => group.Path == "/tenants/tenant-b"), Arg.Any<CancellationToken>()).Returns(true);
        directory.CreateAsync(Arg.Any<TenantGroup>(), Arg.Any<NewTenantUser>(), Arg.Any<CancellationToken>()).Returns(Option<TenantUserId>.Some(created));
        directory.SendInvitationAsync(Arg.Any<TenantUserId>(), Arg.Any<CancellationToken>()).Returns(true);
    }

    [Fact]
    public async Task Staff_invite_an_admin_into_the_tenant_the_route_names()
    {
        var result = await Handle(new InviteTenantAdmin("tenant-b", "Admin@Example.com", "New", "Admin", Guid.CreateVersion7()));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(created.Value);
        await directory.Received(1).CreateAsync(
            Arg.Is<TenantGroup>(group => group.Path == "/tenants/tenant-b"),
            Arg.Is<NewTenantUser>(user => user.Email == "admin@example.com"),
            Arg.Any<CancellationToken>());
        await directory.Received(1).SetRoleAsync(created, TenantRole.CustomerAdmin, Arg.Any<CancellationToken>());
        await directory.Received(1).SendInvitationAsync(created, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("tenant-c")]
    [InlineData("")]
    [InlineData("..")]
    [InlineData("tenant-b/../tenant-a")]
    public async Task A_tenant_the_realm_does_not_have_is_not_found_and_nobody_is_created(string tenantId)
    {
        var result = await Handle(new InviteTenantAdmin(tenantId, "admin@example.com", "New", "Admin", Guid.CreateVersion7()));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("identity.tenant-not-found");
        result.Error.Status.ShouldBe(HttpStatusCode.NotFound);
        await directory.DidNotReceiveWithAnyArgs().CreateAsync(default!, default!, default);
    }

    [Fact]
    public async Task An_address_the_realm_already_has_is_a_conflict()
    {
        directory.CreateAsync(Arg.Any<TenantGroup>(), Arg.Any<NewTenantUser>(), Arg.Any<CancellationToken>()).Returns(Option<TenantUserId>.None);

        var result = await Handle(new InviteTenantAdmin("tenant-b", "taken@example.com", "New", "Admin", Guid.CreateVersion7()));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("identity.user-already-exists");
        result.Error.Status.ShouldBe(HttpStatusCode.Conflict);
        await directory.DidNotReceiveWithAnyArgs().SetRoleAsync(default!, default!, default);
    }

    [Fact]
    public async Task When_the_role_cannot_be_granted_the_new_admin_is_deleted_and_the_failure_rethrown()
    {
        directory.SetRoleAsync(created, Arg.Any<TenantRole>(), Arg.Any<CancellationToken>()).Returns<Task>(_ => throw new HttpRequestException("Keycloak answered 503."));

        var handle = () => Handle(new InviteTenantAdmin("tenant-b", "admin@example.com", "New", "Admin", Guid.CreateVersion7()));

        await Should.ThrowAsync<HttpRequestException>(handle);
        await directory.Received(1).DeleteAsync(created, Arg.Any<CancellationToken>());
    }

    private Task<Result<Guid, ApiError>> Handle(InviteTenantAdmin command) =>
        new InviteTenantAdminHandler(directory, NullLogger<InviteTenantAdminHandler>.Instance)
            .HandleAsync(command, TestContext.Current.CancellationToken);
}
