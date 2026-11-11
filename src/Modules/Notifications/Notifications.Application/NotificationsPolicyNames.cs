namespace FabricPilot.Modules.Notifications.Application;

/// <summary>
/// The names of the module's authorization policies, each <c>notifications.&lt;permission&gt;</c>. They live here, in
/// Application, so a request can name one; <c>NotificationsPolicies</c> in the Api project registers them.
/// </summary>
public static class NotificationsPolicyNames
{
    public const string Manage = "notifications.manage";
}
