namespace FabricPilot.Modules.Notifications.Application;

/// <summary>
/// The names of the module's authorization policies, each <c>notifications.&lt;permission&gt;</c>. They live here, in
/// Application, so a request can name one; <c>NotificationsPolicies</c> in the Api project registers them.
/// </summary>
public static class NotificationsPolicyNames
{
    public const string Manage = "notifications.manage";

    /// <summary>A customer reads the notices addressed to them in their own tenant.</summary>
    public const string ReadOwnNotices = "notifications.read-own-notices";

    /// <summary>Staff read every tenant's rows: name it on an endpoint mapped with <c>AcrossTenants()</c>.</summary>
    public const string ReadAllTenants = "notifications.read-all-tenants";

    /// <summary>Staff write the records shared by all tenants: name it on an endpoint mapped with <c>WritesSharedRecords()</c>.</summary>
    public const string WriteSharedRecords = "notifications.write-shared-records";
}
