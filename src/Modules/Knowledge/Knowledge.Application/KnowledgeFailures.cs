using System.Net;
using FabricPilot.Modules.Knowledge.Domain.Articles;

namespace FabricPilot.Modules.Knowledge.Application;

/// <summary>Everything a knowledge use case refuses, each with its own code.</summary>
public static class KnowledgeFailures
{
    /// <summary>An article rule the request broke: the domain's code, answered as 422, or 409 for a second publishing.</summary>
    public static ApiError From(ArticleError error) => error switch
    {
        InvalidTitle invalidTitle => ApiError.From(invalidTitle, HttpStatusCode.UnprocessableEntity),
        InvalidBody invalidBody => ApiError.From(invalidBody, HttpStatusCode.UnprocessableEntity),
        ArticleAlreadyPublished alreadyPublished => ApiError.From(alreadyPublished, HttpStatusCode.Conflict),
    };

    public static ApiError ArticleNotFound(Guid article) => new ArticleNotFound(article);

    public static ApiError ArticleBeingChanged(Guid article) => new ArticleBeingChanged(article);

    public static ApiError InvalidQuestion(int maximumLength) => new InvalidQuestion(maximumLength);

    public static ApiError SuggestionUnreadable() => new SuggestionUnreadable();
}
