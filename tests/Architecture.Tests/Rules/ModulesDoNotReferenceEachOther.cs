namespace FabricPilot.Architecture.Tests.Rules;

public sealed class ModulesDoNotReferenceEachOther : IArchitectureRule
{
    private const string ModulePrefix = "FabricPilot.Modules.";

    public string Description => "No module references another module's assemblies.";

    public IEnumerable<string> Violations(ArchitectureModel model)
    {
        Ensure.That(model).IsNotNull();

        foreach (var assembly in model.ModuleAssemblies)
        {
            var name = assembly.GetName().Name!;
            var module = ModuleOf(name);
            foreach (var reference in assembly.GetReferencedAssemblies().Select(reference => reference.Name!))
            {
                if (reference.StartsWith(ModulePrefix, StringComparison.Ordinal) && ModuleOf(reference) != module)
                {
                    yield return $"{name} references {reference}.";
                }
            }
        }
    }

    private static string ModuleOf(string assemblyName) => assemblyName[ModulePrefix.Length..].Split('.')[0];
}
