namespace FabricPilot.Architecture.Tests.Rules;

public sealed class AssistantEntitiesKnowNoPersistence : IArchitectureRule
{
    private const string Domain = "FabricPilot.Modules.Assistant.Domain";

    private static readonly string[] Frameworks = ["Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore"];

    public string Description => "The Assistant module is CRUD: its Domain project references neither EF Core nor ASP.NET Core; entities are mapped in Infrastructure.";

    public IEnumerable<string> Violations(ArchitectureModel model)
    {
        Ensure.That(model).IsNotNull();

        var references = model.ModuleAssemblies
            .Where(assembly => assembly.GetName().Name == Domain)
            .SelectMany(assembly => assembly.GetReferencedAssemblies())
            .Select(reference => reference.Name!)
            .Where(name => Frameworks.Any(framework => name.StartsWith(framework, StringComparison.Ordinal)))
            .Order(StringComparer.Ordinal);

        foreach (var reference in references)
        {
            yield return $"{Domain} references {reference}; map entities in Infrastructure.";
        }
    }
}
