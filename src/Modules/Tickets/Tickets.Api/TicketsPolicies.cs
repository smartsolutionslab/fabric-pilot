using FabricPilot.BuildingBlocks.Modules;
using FabricPilot.Modules.Tickets.Application;
using Microsoft.AspNetCore.Authorization;

namespace FabricPilot.Modules.Tickets.Api;

/// <summary>
/// The module's authorization policies, registered under the names in <see cref="TicketsPolicyNames"/>; its
/// endpoints name them.
/// </summary>
public sealed class TicketsPolicies : IModulePolicies
{
    public void AddPolicies(AuthorizationBuilder policies)
    {
        Ensure.That(policies).IsNotNull();

        policies.AddPolicy(TicketsPolicyNames.Manage, policy => policy.RequireRole("manager").RequireClaim("department", "finance"));
    }
}
