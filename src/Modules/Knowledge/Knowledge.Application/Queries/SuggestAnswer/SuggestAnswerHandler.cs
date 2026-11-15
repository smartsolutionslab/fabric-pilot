using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Knowledge.Application.DTOs;
using Microsoft.Extensions.AI;

namespace FabricPilot.Modules.Knowledge.Application.Queries.SuggestAnswer;

/// <summary>
/// Drafts an answer from the published passages nearest the question (retrieval-augmented generation). The draft links
/// only articles whose passages the model was given and cited; when the passages do not answer the question, or the
/// model cites none of them, nothing is drafted and the suggestion says so.
/// </summary>
public sealed class SuggestAnswerHandler(IChatClient model, IArticlePassages passages) : IQueryHandler<SuggestAnswer, Result<SuggestedAnswerDto, ApiError>>
{
    /// <summary>How many passages the model is given: enough for an answer spread over a few articles, few enough to read.</summary>
    public const int PassageCount = 5;

    public async Task<Result<SuggestedAnswerDto, ApiError>> HandleAsync(SuggestAnswer query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        var question = (query.Question ?? string.Empty).Trim();
        if (question.Length == 0 || question.Length > SuggestAnswer.MaximumQuestionLength)
        {
            return Failure(KnowledgeFailures.InvalidQuestion(SuggestAnswer.MaximumQuestionLength));
        }

        var nearest = await passages.NearestAsync(question, PassageCount, cancellationToken);
        if (nearest.Count == 0)
        {
            return Success(SuggestedAnswer.Nothing.ToDto());
        }

        var response = await model.GetResponseAsync(SuggestionPrompt.Messages(question, nearest), SuggestionPrompt.Options(), cancellationToken);
        var read = SuggestionPrompt.Read(response.Text);
        if (!read.HasValue)
        {
            return Failure(KnowledgeFailures.SuggestionUnreadable());
        }

        return Success(Drafted(read.Value, nearest).ToDto());
    }

    /// <summary>The model's draft with the passages it cited, each article once; nothing, when it drafted nothing or cited no passage it was given.</summary>
    private static SuggestedAnswer Drafted(ModelSuggestion suggestion, IReadOnlyList<ArticlePassage> nearest)
    {
        var (answered, answer, cited) = suggestion;
        if (!answered || string.IsNullOrWhiteSpace(answer))
        {
            return SuggestedAnswer.Nothing;
        }

        List<ArticlePassage> sources = [.. cited
            .Where(number => number >= 1 && number <= nearest.Count)
            .Select(number => nearest[number - 1])
            .DistinctBy(passage => passage.Article)];
        if (sources.Count == 0)
        {
            return SuggestedAnswer.Nothing;
        }

        return SuggestedAnswer.Draft(answer.Trim(), sources);
    }
}
