using FabricPilot.BuildingBlocks.Modules;
using FabricPilot.BuildingBlocks.Tenancy;
using FabricPilot.Modules.Knowledge.Application;
using Microsoft.AspNetCore.Authorization;

namespace FabricPilot.Modules.Knowledge.Api;

/// <summary>
/// The module's authorization policies, registered under the names in <see cref="KnowledgePolicyNames"/>; its
/// endpoints name them.
/// </summary>
public sealed class KnowledgePolicies : IModulePolicies
{
    public void AddPolicies(AuthorizationBuilder policies)
    {
        Ensure.That(policies).IsNotNull();

        policies.AddPolicy(KnowledgePolicyNames.Manage, policy => policy.RequireRole("manager").RequireClaim("department", "finance"));
        policies.AddPolicy(KnowledgePolicyNames.SuggestAnswers, policy => policy.RequireRole(CrossTenantRole.Name));
        policies.AddPolicy(KnowledgePolicyNames.ReadAllTenants, policy => policy.RequireRole(CrossTenantRole.Name));
        policies.AddPolicy(KnowledgePolicyNames.WriteSharedRecords, policy => policy.RequireRole(CrossTenantRole.Name));
    }
}
