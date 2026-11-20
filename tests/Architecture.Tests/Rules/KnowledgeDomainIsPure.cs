namespace FabricPilot.Architecture.Tests.Rules;

/// <summary>Per module: the rule checks only the modules that chose ddd, so another module may choose another style.</summary>
public sealed class KnowledgeDomainIsPure : IArchitectureRule
{
    private const string Domain = "FabricPilot.Modules.Knowledge.Domain";

    public string Description => "The Knowledge module is domain-driven: its Domain project references nothing but the BCL, Kernel and BuildingBlocks.Domain.";

    public IEnumerable<string> Violations(ArchitectureModel model)
    {
        Ensure.That(model).IsNotNull();

        foreach (var reference in model.ModuleAssemblies
            .Where(assembly => assembly.GetName().Name == Domain)
            .SelectMany(assembly => assembly.GetReferencedAssemblies())
            .Select(reference => reference.Name!)
            .Where(name => !IsAllowed(name))
            .Order(StringComparer.Ordinal))
        {
            yield return $"{Domain} references {reference}.";
        }
    }

    private static bool IsAllowed(string name) =>
        name is "netstandard" or "mscorlib" or "System" or "FabricPilot.Kernel" or "FabricPilot.BuildingBlocks.Domain"
        || name.StartsWith("System.", StringComparison.Ordinal);
}
