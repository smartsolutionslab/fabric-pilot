using FabricPilot.Modules.Knowledge.Application.Queries.SuggestAnswer;
using Microsoft.Extensions.AI;

namespace FabricPilot.Evals;

/// <summary>
/// The knowledge module's suggested answers, answered through its handler with the example's grounding context as the
/// articles the search finds. What staff would see is what is judged: the draft, or the text saying no article answers,
/// or the failure's message when the model's answer could not be read; and the titles of the articles the draft links,
/// carried beside the text for <see cref="SourcesEvaluator"/>.
/// </summary>
public static class SuggestedAnswers
{
    /// <summary>The response's property holding the linked articles' titles, one per line.</summary>
    public const string SourcesProperty = "sources";

    public static async Task<ChatResponse> AnswerAsync(IChatClient model, EvalExample example, CancellationToken cancellationToken)
    {
        Ensure.That(example).IsNotNull();

        var handler = new SuggestAnswerHandler(model, new EvalPassages(example));
        var result = await handler.HandleAsync(new SuggestAnswer(example.Input), cancellationToken);
        var shown = result.IsSuccess ? result.Value.Text : result.Error.Message;
        IReadOnlyList<string> sources = result.IsSuccess ? [.. result.Value.Sources.Select(source => source.Title)] : [];
        return new ChatResponse(new ChatMessage(ChatRole.Assistant, shown))
        {
            AdditionalProperties = new() { [SourcesProperty] = string.Join('\n', sources) },
        };
    }

    /// <summary>The titles of the articles an answer links; none for an answer that carries none.</summary>
    public static IReadOnlyList<string> SourcesOf(ChatResponse response)
    {
        Ensure.That(response).IsNotNull();

        return response.AdditionalProperties?.TryGetValue(SourcesProperty, out var value) == true && value is string { Length: > 0 } titles
            ? titles.Split('\n')
            : [];
    }
}
