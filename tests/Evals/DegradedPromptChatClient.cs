using FabricPilot.Modules.Knowledge.Application.Queries.SuggestAnswer;
using Microsoft.Extensions.AI;

namespace FabricPilot.Evals;

/// <summary>
/// The model behind <see cref="EvalVariant.Degraded"/>: it swaps the suggested answers' instruction for
/// <see cref="EvalVariants.DegradedInstruction"/> and passes every other message, and every other use case's request, on
/// as it is.
/// </summary>
public sealed class DegradedPromptChatClient(IChatClient model) : DelegatingChatClient(model)
{
    public override Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        base.GetResponseAsync(Degraded(messages), options, cancellationToken);

    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        base.GetStreamingResponseAsync(Degraded(messages), options, cancellationToken);

    private static List<ChatMessage> Degraded(IEnumerable<ChatMessage> messages) =>
        [.. messages.Select(message => message.Role == ChatRole.System && message.Text == SuggestionPrompt.Instruction
            ? new ChatMessage(ChatRole.System, EvalVariants.DegradedInstruction)
            : message)];
}
