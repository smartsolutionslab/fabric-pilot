namespace FabricPilot.Modules.Knowledge.Application;

/// <summary>
/// The names of the module's authorization policies, each <c>knowledge.&lt;permission&gt;</c>. They live here, in
/// Application, so a request can name one; <c>KnowledgePolicies</c> in the Api project registers them.
/// </summary>
public static class KnowledgePolicyNames
{
    public const string Manage = "knowledge.manage";
}
