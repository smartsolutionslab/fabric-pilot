namespace FabricPilot.Modules.Billing.Application;

/// <summary>
/// The names of the module's authorization policies, each <c>billing.&lt;permission&gt;</c>. They live here, in
/// Application, so a request can name one; <c>BillingPolicies</c> in the Api project registers them.
/// </summary>
public static class BillingPolicyNames
{
    public const string Manage = "billing.manage";

    /// <summary>A customer reads their own tenant's invoices and downloads them.</summary>
    public const string ReadInvoices = "billing.read-invoices";

    /// <summary>Staff read every tenant's rows: name it on an endpoint mapped with <c>AcrossTenants()</c>.</summary>
    public const string ReadAllTenants = "billing.read-all-tenants";

    /// <summary>Staff write the records shared by all tenants: name it on an endpoint mapped with <c>WritesSharedRecords()</c>.</summary>
    public const string WriteSharedRecords = "billing.write-shared-records";
}
