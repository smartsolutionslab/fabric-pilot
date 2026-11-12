namespace FabricPilot.Modules.Contracts.Application;

/// <summary>
/// The names of the module's authorization policies, each <c>contracts.&lt;permission&gt;</c>. They live here, in
/// Application, so a request can name one; <c>ContractsPolicies</c> in the Api project registers them.
/// </summary>
public static class ContractsPolicyNames
{
    public const string Manage = "contracts.manage";

    /// <summary>Staff read every tenant's rows: name it on an endpoint mapped with <c>AcrossTenants()</c>.</summary>
    public const string ReadAllTenants = "contracts.read-all-tenants";

    /// <summary>Staff write the records shared by all tenants: name it on an endpoint mapped with <c>WritesSharedRecords()</c>.</summary>
    public const string WriteSharedRecords = "contracts.write-shared-records";
}
