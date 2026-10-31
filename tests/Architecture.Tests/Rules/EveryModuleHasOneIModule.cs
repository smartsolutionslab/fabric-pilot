using FabricPilot.BuildingBlocks.Modules;

namespace FabricPilot.Architecture.Tests.Rules;

public sealed class EveryModuleHasOneIModule : IArchitectureRule
{
    public string Description => "Every module's Api assembly has exactly one IModule.";

    public IEnumerable<string> Violations(ArchitectureModel model)
    {
        Ensure.That(model).IsNotNull();

        foreach (var assembly in model.ModuleAssemblies.Where(assembly => assembly.GetName().Name!.EndsWith(".Api", StringComparison.Ordinal)))
        {
            var count = assembly.GetTypes().Count(type => type is { IsClass: true, IsAbstract: false } && typeof(IModule).IsAssignableFrom(type));
            if (count != 1)
            {
                yield return $"{assembly.GetName().Name} has {count} IModule implementations.";
            }
        }
    }
}
