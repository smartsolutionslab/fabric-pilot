using FabricPilot.BuildingBlocks.Modules;
using FabricPilot.Modules.Dashboards.Application;
using Microsoft.AspNetCore.Authorization;

namespace FabricPilot.Modules.Dashboards.Api;

/// <summary>
/// The module's authorization policies, registered under the names in <see cref="DashboardsPolicyNames"/>; its
/// endpoints name them.
/// </summary>
public sealed class DashboardsPolicies : IModulePolicies
{
    public void AddPolicies(AuthorizationBuilder policies)
    {
        Ensure.That(policies).IsNotNull();

        policies.AddPolicy(DashboardsPolicyNames.Manage, policy => policy.RequireRole("manager").RequireClaim("department", "finance"));
    }
}
