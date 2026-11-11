using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FabricPilot.Evals;

/// <summary>
/// The project's own model, registered as the host registers it, for the evals to answer with and to judge with.
/// Its values are the AppHost's parameters, read from the AppHost's user secrets (dotnet user-secrets set
/// Parameters:... --project src/AppHost) and from the environment (<c>Parameters__...</c>, as the evals workflow sets
/// them); a parameter not set takes the AppHost's default, and a missing key fails naming its parameter.
/// </summary>
public static class ProjectModel
{
    public static ServiceProvider Services()
    {
        var parameters = new ConfigurationBuilder()
            .AddUserSecrets("fabric-fabric-pilot-apphost")
            .AddEnvironmentVariables()
            .Build();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureOpenAI:Endpoint"] = Parameter(parameters, "azure-openai-endpoint", string.Empty),
                ["AzureOpenAI:Deployment"] = Parameter(parameters, "azure-openai-deployment", "gpt-5-mini"),
                ["AzureOpenAI:ApiKey"] = Parameter(parameters, "azure-openai-api-key", string.Empty),
            })
            .Build();

        var services = new ServiceCollection().AddLogging();
        services.AddAzureOpenAIChatClient(configuration);
        return services.BuildServiceProvider();
    }

    /// <summary>The AppHost's parameter, or its default: empty for one without a default, which the client then reports.</summary>
    private static string Parameter(IConfiguration configuration, string parameter, string fallback) =>
        configuration[$"Parameters:{parameter}"] is { Length: > 0 } value ? value : fallback;
}
