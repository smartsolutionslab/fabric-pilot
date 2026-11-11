namespace FabricPilot.Modules.Assistant.Application;

/// <summary>
/// The names of the module's authorization policies, each <c>assistant.&lt;permission&gt;</c>. They live here, in
/// Application, so a request can name one; <c>AssistantPolicies</c> in the Api project registers them.
/// </summary>
public static class AssistantPolicyNames
{
    public const string Manage = "assistant.manage";
}
