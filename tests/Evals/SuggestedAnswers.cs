using FabricPilot.Modules.Knowledge.Application.Queries.SuggestAnswer;
using Microsoft.Extensions.AI;

namespace FabricPilot.Evals;

/// <summary>
/// The knowledge module's suggested answers, answered through its handler with the example's grounding context as the
/// article the search finds. What staff would see is what is judged: the draft, or the text saying no article answers,
/// or the failure's message when the model's answer could not be read.
/// </summary>
public static class SuggestedAnswers
{
    public static async Task<ChatResponse> AnswerAsync(IChatClient model, EvalExample example, CancellationToken cancellationToken)
    {
        Ensure.That(example).IsNotNull();

        var handler = new SuggestAnswerHandler(model, new EvalPassages(example));
        var result = await handler.HandleAsync(new SuggestAnswer(example.Input), cancellationToken);
        var shown = result.IsSuccess ? result.Value.Text : result.Error.Message;
        return new ChatResponse(new ChatMessage(ChatRole.Assistant, shown));
    }
}
