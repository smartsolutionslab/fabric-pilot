using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FabricPilot.BuildingBlocks.Modules;

public static class ModuleInfrastructure
{
    /// <summary>Everything a module needs from infrastructure; each choice that needs something per module adds it here.</summary>
    public static IServiceCollection AddModuleInfrastructure<TModule>(this IServiceCollection services, IConfiguration configuration)
        where TModule : IModule
    {
        Ensure.That(services).IsNotNull();
        Ensure.That(configuration).IsNotNull();
        services.AddModuleDbContext<TModule>(configuration);
        services.AddModuleHandlers<TModule>();
        services.AddModuleUnitOfWork<TModule>();
        return services;
    }
}
