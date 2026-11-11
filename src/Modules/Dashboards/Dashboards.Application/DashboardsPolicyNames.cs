namespace FabricPilot.Modules.Dashboards.Application;

/// <summary>
/// The names of the module's authorization policies, each <c>dashboards.&lt;permission&gt;</c>. They live here, in
/// Application, so a request can name one; <c>DashboardsPolicies</c> in the Api project registers them.
/// </summary>
public static class DashboardsPolicyNames
{
    public const string Manage = "dashboards.manage";
}
