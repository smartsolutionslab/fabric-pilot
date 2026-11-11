using FabricPilot.BuildingBlocks.Modules;
using FabricPilot.Modules.Documents.Application;
using Microsoft.AspNetCore.Authorization;

namespace FabricPilot.Modules.Documents.Api;

/// <summary>
/// The module's authorization policies, registered under the names in <see cref="DocumentsPolicyNames"/>; its
/// endpoints name them.
/// </summary>
public sealed class DocumentsPolicies : IModulePolicies
{
    public void AddPolicies(AuthorizationBuilder policies)
    {
        Ensure.That(policies).IsNotNull();

        policies.AddPolicy(DocumentsPolicyNames.Manage, policy => policy.RequireRole("manager").RequireClaim("department", "finance"));
    }
}
