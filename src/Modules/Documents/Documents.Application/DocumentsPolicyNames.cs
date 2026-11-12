namespace FabricPilot.Modules.Documents.Application;

/// <summary>
/// The names of the module's authorization policies, each <c>documents.&lt;permission&gt;</c>. They live here, in
/// Application, so a request can name one; <c>DocumentsPolicies</c> in the Api project registers them.
/// </summary>
public static class DocumentsPolicyNames
{
    public const string Manage = "documents.manage";

    /// <summary>Staff read every tenant's rows: name it on an endpoint mapped with <c>AcrossTenants()</c>.</summary>
    public const string ReadAllTenants = "documents.read-all-tenants";

    /// <summary>Staff write the records shared by all tenants: name it on an endpoint mapped with <c>WritesSharedRecords()</c>.</summary>
    public const string WriteSharedRecords = "documents.write-shared-records";
}
