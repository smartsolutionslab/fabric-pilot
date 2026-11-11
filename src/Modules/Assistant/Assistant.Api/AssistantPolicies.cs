using FabricPilot.BuildingBlocks.Modules;
using FabricPilot.Modules.Assistant.Application;
using Microsoft.AspNetCore.Authorization;

namespace FabricPilot.Modules.Assistant.Api;

/// <summary>
/// The module's authorization policies, registered under the names in <see cref="AssistantPolicyNames"/>; its
/// endpoints name them.
/// </summary>
public sealed class AssistantPolicies : IModulePolicies
{
    public void AddPolicies(AuthorizationBuilder policies)
    {
        Ensure.That(policies).IsNotNull();

        policies.AddPolicy(AssistantPolicyNames.Manage, policy => policy.RequireRole("manager").RequireClaim("department", "finance"));
    }
}
