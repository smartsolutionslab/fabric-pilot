using FabricPilot.BuildingBlocks.Modules;
using FabricPilot.BuildingBlocks.Tenancy;
using FabricPilot.Modules.Billing.Application;
using Microsoft.AspNetCore.Authorization;

namespace FabricPilot.Modules.Billing.Api;

/// <summary>
/// The module's authorization policies, registered under the names in <see cref="BillingPolicyNames"/>; its
/// endpoints name them.
/// </summary>
public sealed class BillingPolicies : IModulePolicies
{
    public void AddPolicies(AuthorizationBuilder policies)
    {
        Ensure.That(policies).IsNotNull();

        policies.AddPolicy(BillingPolicyNames.Manage, policy => policy.RequireRole("manager").RequireClaim("department", "finance"));
        // customer-admin includes customer-user in the realm, so a tenant's admin holds this too.
        policies.AddPolicy(BillingPolicyNames.ReadInvoices, policy => policy.RequireRole("customer-user"));
        policies.AddPolicy(BillingPolicyNames.ReadAllTenants, policy => policy.RequireRole(CrossTenantRole.Name));
        policies.AddPolicy(BillingPolicyNames.WriteSharedRecords, policy => policy.RequireRole(CrossTenantRole.Name));
    }
}
