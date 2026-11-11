using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Assistant.Application.Agents;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace FabricPilot.Modules.Assistant.Application.Queries.TalkToTheAssistant;

/// <summary>
/// Runs the assistant on the conversation and hands back its updates as they come: text, tool calls and their results.
/// Every update is the model's output, untrusted input: the panel shows it as text and never executes it.
/// </summary>
public sealed class TalkToTheAssistantHandler(IChatClient model, IDispatcher dispatcher) : IQueryHandler<TalkToTheAssistant, Result<IAsyncEnumerable<ChatResponseUpdate>, ApiError>>
{
    public Task<Result<IAsyncEnumerable<ChatResponseUpdate>, ApiError>> HandleAsync(TalkToTheAssistant query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        var updates = AssistantAgent.Create(model, dispatcher).RunStreamingAsync(query.Messages, cancellationToken: cancellationToken).AsChatResponseUpdatesAsync();
        return Task.FromResult<Result<IAsyncEnumerable<ChatResponseUpdate>, ApiError>>(Success(updates));
    }
}
