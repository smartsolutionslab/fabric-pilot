using FabricPilot.Host.Agents;

namespace Microsoft.AspNetCore.Builder;

public static class AgentUiEndpoints
{
    /// <summary>
    /// <c>POST /agents/{name}</c>, the agent panel's one endpoint, for signed-in users: it reads the AG-UI
    /// input, sends the named agent's query through the dispatcher and streams the agent's updates back as AG-UI events.
    /// The panel speaks AG-UI rather than the OpenAPI document's REST, so the endpoint is not described there.
    /// </summary>
    public static WebApplication MapAgentUi(this WebApplication app)
    {
        Ensure.That(app).IsNotNull();

        app.MapPost("/agents/{name}", AgentRuns.RunAsync)
            .RequireAuthorization()
            .ExcludeFromDescription();
        return app;
    }
}
