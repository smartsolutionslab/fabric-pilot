using FabricPilot.Modules.Contracts.Application;
using FabricPilot.Modules.Contracts.Infrastructure;

namespace Microsoft.Extensions.DependencyInjection;

public static class ContractsStorage
{
    /// <summary>The contracts table, read through the module's DbContext.</summary>
    public static IServiceCollection AddContractsStorage(this IServiceCollection services) =>
        services.AddScoped<IContractStore, ContractStore>();
}
