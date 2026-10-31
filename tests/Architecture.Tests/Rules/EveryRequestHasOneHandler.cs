namespace FabricPilot.Architecture.Tests.Rules;

public sealed class EveryRequestHasOneHandler : IArchitectureRule
{
    private static readonly string[] Requests = ["FabricPilot.BuildingBlocks.Messaging.ICommand`1", "FabricPilot.BuildingBlocks.Messaging.IQuery`1"];
    private static readonly string[] Handlers = ["FabricPilot.BuildingBlocks.Messaging.ICommandHandler`2", "FabricPilot.BuildingBlocks.Messaging.IQueryHandler`2"];

    public string Description => "Every command and query in an Application assembly has exactly one handler there.";

    public IEnumerable<string> Violations(ArchitectureModel model)
    {
        Ensure.That(model).IsNotNull();

        foreach (var assembly in model.ModuleAssemblies.Where(assembly => assembly.GetName().Name!.EndsWith(".Application", StringComparison.Ordinal)))
        {
            var types = assembly.GetTypes();
            var handled = types
                .SelectMany(type => type.GetInterfaces())
                .Where(contract => contract.IsGenericType && Handlers.Contains(contract.GetGenericTypeDefinition().FullName))
                .Select(contract => contract.GetGenericArguments()[0])
                .ToList();

            foreach (var request in types.Where(type => !type.IsInterface && type.GetInterfaces().Any(contract => contract.IsGenericType && Requests.Contains(contract.GetGenericTypeDefinition().FullName))))
            {
                var count = handled.Count(type => type == request);
                if (count != 1)
                {
                    yield return $"{request.FullName} has {count} handlers.";
                }
            }
        }
    }
}
