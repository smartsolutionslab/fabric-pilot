namespace FabricPilot.Modules.Knowledge.Application;

/// <summary>
/// The names of the module's authorization policies, each <c>knowledge.&lt;permission&gt;</c>. They live here, in
/// Application, so a request can name one; <c>KnowledgePolicies</c> in the Api project registers them.
/// </summary>
public static class KnowledgePolicyNames
{
    public const string Manage = "knowledge.manage";

    /// <summary>Staff ask the model for an answer to a ticket, drafted from the knowledge base.</summary>
    public const string SuggestAnswers = "knowledge.suggest-answers";

    /// <summary>Staff read every tenant's rows: name it on an endpoint mapped with <c>AcrossTenants()</c>.</summary>
    public const string ReadAllTenants = "knowledge.read-all-tenants";

    /// <summary>Staff write the records shared by all tenants: name it on an endpoint mapped with <c>WritesSharedRecords()</c>.</summary>
    public const string WriteSharedRecords = "knowledge.write-shared-records";
}
