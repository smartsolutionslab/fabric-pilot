using System.Text.Json;
using FabricPilot.BuildingBlocks.Agents;
using Microsoft.Extensions.AI;

namespace FabricPilot.Modules.Assistant.Application.Queries.TalkToTheAssistant;

/// <summary>One run of a conversation with the assistant, as the agent panel posts it to <c>/agents/assistant</c>.</summary>
public sealed record TalkToTheAssistant(IReadOnlyList<ChatMessage> Messages, JsonElement? State) : IAgentConversation<TalkToTheAssistant>
{
    public static string AgentName => "assistant";

    public static TalkToTheAssistant Start(IReadOnlyList<ChatMessage> messages, JsonElement? state) => new(messages, state);
}
