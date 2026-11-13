using System.Net;
using FabricPilot.Modules.Identity.Application;
using FabricPilot.Modules.Identity.Application.Commands.ResendInvitation;
using Microsoft.Extensions.Logging.Abstractions;

namespace FabricPilot.Modules.Identity.Tests.Commands;

public class ResendInvitationTests
{
    private static readonly string[] TenantA = ["/tenants/tenant-a"];
    private static readonly string[] TenantB = ["/tenants/tenant-b"];

    [Fact]
    public async Task A_user_of_the_callers_tenant_is_sent_the_invitation_again()
    {
        var user = DirectoryUsers.User(DirectoryUsers.NewId(), TenantA, "customer-user");
        var directory = DirectoryUsers.Knowing(user);
        directory.SendInvitationAsync(user.Id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await Handle(directory, "tenant-a", new ResendInvitation(user.Id.Value, Guid.CreateVersion7()));

        result.IsSuccess.ShouldBeTrue();
        await directory.Received(1).SendInvitationAsync(user.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_user_of_another_tenant_is_not_found_and_sent_nothing()
    {
        var user = DirectoryUsers.User(DirectoryUsers.NewId(), TenantB, "customer-user");
        var directory = DirectoryUsers.Knowing(user);

        var result = await Handle(directory, "tenant-a", new ResendInvitation(user.Id.Value, Guid.CreateVersion7()));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("identity.user-not-found");
        await directory.DidNotReceiveWithAnyArgs().SendInvitationAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task An_invitation_the_realm_could_not_send_is_reported()
    {
        var user = DirectoryUsers.User(DirectoryUsers.NewId(), TenantA, "customer-user");
        var directory = DirectoryUsers.Knowing(user);
        directory.SendInvitationAsync(user.Id, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handle(directory, "tenant-a", new ResendInvitation(user.Id.Value, Guid.CreateVersion7()));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("identity.invitation-not-sent");
        result.Error.Status.ShouldBe(HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task Without_a_tenant_the_caller_is_refused_and_nothing_reaches_the_directory()
    {
        var directory = DirectoryUsers.Knowing();

        var result = await Handle(directory, null, new ResendInvitation(Guid.CreateVersion7(), Guid.CreateVersion7()));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("identity.no-tenant");
        directory.ReceivedCalls().ShouldBeEmpty();
    }

    private static Task<Result<Unit, ApiError>> Handle(ITenantUserDirectory directory, string? tenantId, ResendInvitation command) =>
        new ResendInvitationHandler(DirectoryUsers.Tenant(tenantId), directory, NullLogger<ResendInvitationHandler>.Instance)
            .HandleAsync(command, TestContext.Current.CancellationToken);
}
