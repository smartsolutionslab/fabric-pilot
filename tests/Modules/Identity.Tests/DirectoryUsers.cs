using FabricPilot.BuildingBlocks.Tenancy;
using FabricPilot.Modules.Identity.Application;
using FabricPilot.Modules.Identity.Domain.TenantUsers;

namespace FabricPilot.Modules.Identity.Tests;

/// <summary>Users as the directory answers them, and the directory and tenant the handlers are given.</summary>
internal static class DirectoryUsers
{
    public static TenantUser User(TenantUserId id, string[] groups, params string[] realmRoles) =>
        new(id, $"{id.Value:N}@example.com", "First", "Last", groups, realmRoles);

    public static TenantUserId NewId() => new(Guid.CreateVersion7());

    public static ITenantContext Tenant(string? tenantId)
    {
        var tenant = Substitute.For<ITenantContext>();
        tenant.TenantId.Returns(tenantId);
        return tenant;
    }

    /// <summary>A directory that knows the user, as FindAsync answers for them.</summary>
    public static ITenantUserDirectory Knowing(params TenantUser[] users)
    {
        var directory = Substitute.For<ITenantUserDirectory>();
        directory.FindAsync(Arg.Any<TenantUserId>(), Arg.Any<CancellationToken>()).Returns(Option<TenantUser>.None);
        foreach (var user in users)
        {
            directory.FindAsync(user.Id, Arg.Any<CancellationToken>()).Returns(Option<TenantUser>.Some(user));
        }

        return directory;
    }
}
