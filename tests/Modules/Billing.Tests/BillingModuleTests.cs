using FabricPilot.Modules.Billing.Api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FabricPilot.Modules.Billing.Tests;

public class BillingModuleTests
{
    /// <summary>Starts from what every host provides (logging), adds the module, and validates every registration.</summary>
    [Fact]
    public void The_module_can_resolve_every_service_it_registers()
    {
        var services = new ServiceCollection().AddLogging();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:database"] = "Host=localhost" })
            .Build();

        new BillingModule().AddServices(services, configuration);
        var buildContainer = () => services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }).Dispose();

        Should.NotThrow(buildContainer);
    }
}
