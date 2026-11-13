namespace FabricPilot.Modules.Identity.Application;

/// <summary>
/// The names of the module's authorization policies, each <c>identity.&lt;permission&gt;</c>. They live here, in
/// Application, so a request can name one; <c>IdentityPolicies</c> in the Api project registers them.
/// </summary>
public static class IdentityPolicyNames
{
    public const string Manage = "identity.manage";

    /// <summary>Staff read every tenant's rows: name it on an endpoint mapped with <c>AcrossTenants()</c>.</summary>
    public const string ReadAllTenants = "identity.read-all-tenants";

    /// <summary>Staff write the records shared by all tenants: name it on an endpoint mapped with <c>WritesSharedRecords()</c>.</summary>
    public const string WriteSharedRecords = "identity.write-shared-records";
}
