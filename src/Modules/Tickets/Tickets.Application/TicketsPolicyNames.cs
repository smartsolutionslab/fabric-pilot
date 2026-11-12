namespace FabricPilot.Modules.Tickets.Application;

/// <summary>
/// The names of the module's authorization policies, each <c>tickets.&lt;permission&gt;</c>. They live here, in
/// Application, so a request can name one; <c>TicketsPolicies</c> in the Api project registers them.
/// </summary>
public static class TicketsPolicyNames
{
    public const string Manage = "tickets.manage";
}
