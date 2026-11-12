namespace FabricPilot.Modules.Knowledge.Domain.Articles;

/// <summary>Every rule an article can break, each with its own code.</summary>
public static class ArticleErrors
{
    public static ArticleError InvalidTitle(int maximum) => new InvalidTitle(maximum);

    public static ArticleError InvalidBody(int maximum) => new InvalidBody(maximum);

    public static ArticleError AlreadyPublished(ArticleId article) => new ArticleAlreadyPublished(article);
}
