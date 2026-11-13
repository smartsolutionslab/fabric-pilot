using FabricPilot.BuildingBlocks.Tenancy;
using FabricPilot.Modules.Identity.Domain.TenantUsers;

namespace FabricPilot.Modules.Identity.Application;

/// <summary>Which users a tenant's admin, or staff acting for the tenant, may manage.</summary>
public static class TenantMembers
{
    /// <summary>
    /// The user, when they are a member of exactly this tenant's group and of no other group, and not staff; None for
    /// everyone else, including users the realm does not have.
    /// </summary>
    public static async Task<Option<TenantUser>> FindAsync(ITenantUserDirectory directory, TenantGroup group, Guid user, CancellationToken cancellationToken)
    {
        Ensure.That(directory).IsNotNull();
        Ensure.That(group).IsNotNull();

        var found = await directory.FindAsync(new TenantUserId(user), cancellationToken);
        if (found.HasValue && found.Value.Groups is [var only] && only == group.Path && !found.Value.RealmRoles.Contains(CrossTenantRole.Name, StringComparer.Ordinal))
        {
            return found;
        }

        return Option<TenantUser>.None;
    }

    /// <summary>The tenant's group from the caller's token, never from the request; None without exactly one tenant.</summary>
    public static Option<TenantGroup> CallersGroup(ITenantContext tenant)
    {
        Ensure.That(tenant).IsNotNull();
        return tenant.TenantId is { } tenantId ? TenantGroup.For(tenantId) : Option<TenantGroup>.None;
    }
}
