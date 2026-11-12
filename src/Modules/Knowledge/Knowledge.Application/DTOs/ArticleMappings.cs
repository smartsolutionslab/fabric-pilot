using System.Linq.Expressions;
using FabricPilot.Modules.Knowledge.Domain.Articles;

namespace FabricPilot.Modules.Knowledge.Application.DTOs;

public static class ArticleMappings
{
    public static ArticleDto ToArticleDto(this Article article)
    {
        Ensure.That(article).IsNotNull();
        return new ArticleDto(article.Id.Value, article.Title, article.Body, article.State.ToString(), article.ChangedAt, article.PublishedAt);
    }

    /// <summary>For an EF <c>Select</c>: the text is never read for a list.</summary>
    public static Expression<Func<Article, ArticleSummaryDto>> ToArticleSummaryDtoExpression() =>
        article => new ArticleSummaryDto(article.Id.Value, article.Title, article.State.ToString(), article.ChangedAt, article.PublishedAt);
}
