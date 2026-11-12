using FabricPilot.BuildingBlocks.Modules;
using FabricPilot.BuildingBlocks.Tenancy;
using FabricPilot.Modules.Contracts.Application;
using Microsoft.AspNetCore.Authorization;

namespace FabricPilot.Modules.Contracts.Api;

/// <summary>
/// The module's authorization policies, registered under the names in <see cref="ContractsPolicyNames"/>; its
/// endpoints name them.
/// </summary>
public sealed class ContractsPolicies : IModulePolicies
{
    public void AddPolicies(AuthorizationBuilder policies)
    {
        Ensure.That(policies).IsNotNull();

        policies.AddPolicy(ContractsPolicyNames.Manage, policy => policy.RequireRole("manager").RequireClaim("department", "finance"));
        policies.AddPolicy(ContractsPolicyNames.ReadAllTenants, policy => policy.RequireRole(CrossTenantRole.Name));
        policies.AddPolicy(ContractsPolicyNames.WriteSharedRecords, policy => policy.RequireRole(CrossTenantRole.Name));
    }
}
