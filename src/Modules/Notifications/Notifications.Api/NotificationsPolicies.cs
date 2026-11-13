using FabricPilot.BuildingBlocks.Modules;
using FabricPilot.BuildingBlocks.Tenancy;
using FabricPilot.Modules.Notifications.Application;
using Microsoft.AspNetCore.Authorization;

namespace FabricPilot.Modules.Notifications.Api;

/// <summary>
/// The module's authorization policies, registered under the names in <see cref="NotificationsPolicyNames"/>; its
/// endpoints name them.
/// </summary>
public sealed class NotificationsPolicies : IModulePolicies
{
    public void AddPolicies(AuthorizationBuilder policies)
    {
        Ensure.That(policies).IsNotNull();

        policies.AddPolicy(NotificationsPolicyNames.Manage, policy => policy.RequireRole("manager").RequireClaim("department", "finance"));
        // customer-admin includes customer-user in the realm, so a tenant's admin holds this too.
        policies.AddPolicy(NotificationsPolicyNames.ReadOwnNotices, policy => policy.RequireRole("customer-user"));
        policies.AddPolicy(NotificationsPolicyNames.ReadAllTenants, policy => policy.RequireRole(CrossTenantRole.Name));
        policies.AddPolicy(NotificationsPolicyNames.WriteSharedRecords, policy => policy.RequireRole(CrossTenantRole.Name));
    }
}
