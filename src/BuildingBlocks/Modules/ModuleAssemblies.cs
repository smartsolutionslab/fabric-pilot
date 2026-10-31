using System.Reflection;

namespace FabricPilot.BuildingBlocks.Modules;

public static class ModuleAssemblies
{
    /// <summary>The assembly of one layer (Domain, Application, Infrastructure, Api) of the module <typeparamref name="TModule"/> belongs to.</summary>
    public static Assembly Of<TModule>(string layer)
        where TModule : IModule =>
        Of(typeof(TModule), layer);

    public static Assembly Of(IModule module, string layer)
    {
        Ensure.That(module).IsNotNull();
        return Of(module.GetType(), layer);
    }

    private static Assembly Of(Type module, string layer)
    {
        var api = module.Assembly.GetName().Name!;
        return Assembly.Load(api[..api.LastIndexOf('.')] + "." + layer);
    }
}
