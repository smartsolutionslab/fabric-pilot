namespace FabricPilot.Modules.Tickets.Application;

/// <summary>
/// The names of the module's authorization policies, each <c>tickets.&lt;permission&gt;</c>. They live here, in
/// Application, so a request can name one; <c>TicketsPolicies</c> in the Api project registers them.
/// </summary>
public static class TicketsPolicyNames
{
    public const string Manage = "tickets.manage";

    /// <summary>Staff read every tenant's rows: name it on an endpoint mapped with <c>AcrossTenants()</c>.</summary>
    public const string ReadAllTenants = "tickets.read-all-tenants";

    /// <summary>Staff write the records shared by all tenants: name it on an endpoint mapped with <c>WritesSharedRecords()</c>.</summary>
    public const string WriteSharedRecords = "tickets.write-shared-records";
}
