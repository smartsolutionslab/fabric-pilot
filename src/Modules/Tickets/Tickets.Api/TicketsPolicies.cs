using FabricPilot.BuildingBlocks.Modules;
using FabricPilot.BuildingBlocks.Tenancy;
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
        // customer-admin includes customer-user in the realm, so a tenant's admin holds this too.
        policies.AddPolicy(TicketsPolicyNames.RaiseAndRead, policy => policy.RequireRole("customer-user"));
        policies.AddPolicy(TicketsPolicyNames.ReadAllTenants, policy => policy.RequireRole(CrossTenantRole.Name));
        policies.AddPolicy(TicketsPolicyNames.WriteSharedRecords, policy => policy.RequireRole(CrossTenantRole.Name));
    }
}
