namespace FabricPilot.Modules.Contracts.Application;

/// <summary>
/// The names of the module's authorization policies, each <c>contracts.&lt;permission&gt;</c>. They live here, in
/// Application, so a request can name one; <c>ContractsPolicies</c> in the Api project registers them.
/// </summary>
public static class ContractsPolicyNames
{
    public const string Manage = "contracts.manage";
}
