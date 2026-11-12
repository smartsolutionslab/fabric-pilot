namespace FabricPilot.Modules.Knowledge.Domain.Articles;

public sealed record ArticleAlreadyPublished(ArticleId Article)
    : DomainError("knowledge.article-already-published", $"The article {Article.Value} is published already.");
