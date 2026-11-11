namespace FabricPilot.Modules.Documents.Application;

/// <summary>
/// The names of the module's authorization policies, each <c>documents.&lt;permission&gt;</c>. They live here, in
/// Application, so a request can name one; <c>DocumentsPolicies</c> in the Api project registers them.
/// </summary>
public static class DocumentsPolicyNames
{
    public const string Manage = "documents.manage";
}
