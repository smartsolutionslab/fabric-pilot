namespace FabricPilot.Modules.Knowledge.Application.Queries.SuggestAnswer;

/// <summary>A drafted answer and the passages of the articles it is drafted from, one per article; or no draft, saying why.</summary>
public sealed record SuggestedAnswer(bool Drafted, string Text, IReadOnlyList<ArticlePassage> Sources)
{
    public const string NoArticleAnswers = "No article in the knowledge base answers this ticket, so no answer was drafted. Write the answer yourself.";

    /// <summary>No article answers the question: nothing is drafted rather than an answer the knowledge base does not hold.</summary>
    public static SuggestedAnswer Nothing { get; } = new(false, NoArticleAnswers, []);

    public static SuggestedAnswer Draft(string text, IReadOnlyList<ArticlePassage> sources) => new(true, text, sources);
}
