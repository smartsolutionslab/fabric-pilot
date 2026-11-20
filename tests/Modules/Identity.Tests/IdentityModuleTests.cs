using FabricPilot.Modules.Identity.Api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FabricPilot.Modules.Identity.Tests;

public class IdentityModuleTests
{
    /// <summary>Starts from what every host provides (logging), adds the module, and validates every registration.</summary>
    [Fact]
    public void The_module_can_resolve_every_service_it_registers()
    {
        var services = new ServiceCollection().AddLogging();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:database"] = "Host=localhost" })
            .Build();

        // The host provides the dispatcher, which a handler running an agent takes to send the agent's tool calls.
        services.AddDispatcher();

        // The host registers every module's vector index, the tenant source it reads and the project's IEmbeddingGenerator; a module's test stands in for the generator and never calls a real model.
        services.AddModuleVectorIndexes(configuration);
        services.AddTenancy();
        services.AddSingleton<Microsoft.Extensions.AI.IEmbeddingGenerator<string, Microsoft.Extensions.AI.Embedding<float>>>(_ => new FabricPilot.Tests.Fakes.FakeEmbeddingGenerator(FabricPilot.BuildingBlocks.Rag.VectorTables.Dimensions));
        // The host registers the project's IChatClient; a module's test stands in for it and never calls a real model.
        services.AddSingleton<Microsoft.Extensions.AI.IChatClient>(_ => new FabricPilot.Tests.Fakes.FakeChatClient("unused"));

        new IdentityModule().AddServices(services, configuration);
        var buildContainer = () => services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }).Dispose();

        Should.NotThrow(buildContainer);
    }

    /// <summary>Another module (tickets) checks an assignee through the contract; the identity module is what answers it.</summary>
    [Fact]
    public void The_module_provides_the_staff_directory_other_modules_ask()
    {
        var services = new ServiceCollection().AddLogging();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:database"] = "Host=localhost" })
            .Build();

        new IdentityModule().AddServices(services, configuration);

        services.ShouldContain(service => service.ServiceType == typeof(FabricPilot.Contracts.Identity.IStaffDirectory));
    }
}
