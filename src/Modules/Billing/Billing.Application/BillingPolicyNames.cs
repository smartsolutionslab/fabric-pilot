namespace FabricPilot.Modules.Billing.Application;

/// <summary>
/// The names of the module's authorization policies, each <c>billing.&lt;permission&gt;</c>. They live here, in
/// Application, so a request can name one; <c>BillingPolicies</c> in the Api project registers them.
/// </summary>
public static class BillingPolicyNames
{
    public const string Manage = "billing.manage";
}
