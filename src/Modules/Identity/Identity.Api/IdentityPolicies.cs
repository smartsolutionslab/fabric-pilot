using FabricPilot.BuildingBlocks.Modules;
using FabricPilot.BuildingBlocks.Tenancy;
using FabricPilot.Modules.Identity.Application;
using Microsoft.AspNetCore.Authorization;

namespace FabricPilot.Modules.Identity.Api;

/// <summary>
/// The module's authorization policies, registered under the names in <see cref="IdentityPolicyNames"/>; its
/// endpoints name them.
/// </summary>
public sealed class IdentityPolicies : IModulePolicies
{
    public void AddPolicies(AuthorizationBuilder policies)
    {
        Ensure.That(policies).IsNotNull();

        policies.AddPolicy(IdentityPolicyNames.Manage, policy => policy.RequireRole("manager").RequireClaim("department", "finance"));
        policies.AddPolicy(IdentityPolicyNames.ReadAllTenants, policy => policy.RequireRole(CrossTenantRole.Name));
        policies.AddPolicy(IdentityPolicyNames.WriteSharedRecords, policy => policy.RequireRole(CrossTenantRole.Name));
    }
}
