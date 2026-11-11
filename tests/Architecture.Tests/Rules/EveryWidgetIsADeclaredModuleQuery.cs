using FabricPilot.BuildingBlocks.Dashboards;

namespace FabricPilot.Architecture.Tests.Rules;

/// <summary>
/// A dashboard reaches only queries a module declares in its <see cref="IModuleWidgets"/>. The build fails on exactly
/// what would stop the host at startup, because both ask <see cref="WidgetCatalog"/>. It cannot see whether a query
/// reads only what its widget's viewers may see beyond the policy it names.
/// </summary>
public sealed class EveryWidgetIsADeclaredModuleQuery : IArchitectureRule
{
    private const string ModulePrefix = "FabricPilot.Modules.";

    private const string ApplicationSuffix = ".Application";

    private const string ApiSuffix = ".Api";

    public string Description => "Every widget a module declares runs a query of that module's Application project with exactly one handler and names only a policy its own module registers, and only an Application project declares widgets.";

    public IEnumerable<string> Violations(ArchitectureModel model)
    {
        Ensure.That(model).IsNotNull();

        // The building blocks too: a declaration there would belong to no module and be offered by none.
        List<string> problems = [.. WidgetCatalog.Misplaced(typeof(IModuleWidgets).Assembly)];
        foreach (var assembly in model.ModuleAssemblies)
        {
            problems.AddRange(WidgetCatalog.Misplaced(assembly));
            var name = assembly.GetName().Name!;
            if (!name.EndsWith(ApplicationSuffix, StringComparison.Ordinal))
            {
                continue;
            }

            var module = name[ModulePrefix.Length..^ApplicationSuffix.Length];
            var api = model.ModuleAssemblies.Single(candidate => candidate.GetName().Name == ModulePrefix + module + ApiSuffix);
            problems.AddRange(WidgetCatalog.Scan(module, assembly, api).Problems);
        }

        return problems.Distinct(StringComparer.Ordinal);
    }
}
