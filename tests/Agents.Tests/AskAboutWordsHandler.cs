using FabricPilot.BuildingBlocks.Messaging;
using Microsoft.Extensions.AI;

namespace FabricPilot.Agents.Tests;

/// <summary>
/// Runs the agent on the question. The answer is the model's output: untrusted input, returned as text and never
/// executed or put into SQL or markup.
/// </summary>
public sealed class AskAboutWordsHandler(IChatClient model, IDispatcher dispatcher) : IQueryHandler<AskAboutWords, Result<string, ApiError>>
{
    public async Task<Result<string, ApiError>> HandleAsync(AskAboutWords query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        var response = await WordsAgent.Create(model, dispatcher).RunAsync(query.Question, cancellationToken: cancellationToken);
        return Success(response.Text);
    }
}
