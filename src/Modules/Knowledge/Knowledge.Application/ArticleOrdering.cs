using System.Diagnostics;
using System.Linq.Expressions;
using FabricPilot.Modules.Knowledge.Domain.Articles;

namespace FabricPilot.Modules.Knowledge.Application;

public static class ArticleOrdering
{
    /// <summary>Sorted as asked, then by id in the same direction, so articles with the same key keep one order across pages.</summary>
    public static IOrderedQueryable<Article> SortedBy(this IQueryable<Article> articles, Sorting<ArticleSortField> sorting)
    {
        Ensure.That(articles).IsNotNull();
        Ensure.That(sorting).IsNotNull();

        return sorting.Field switch
        {
            ArticleSortField.ChangedAt => By(articles, article => article.ChangedAt, sorting.Direction),
            ArticleSortField.Title => By(articles, article => article.Title, sorting.Direction),
            _ => throw new UnreachableException($"The list query refuses the sort field {sorting.Field}."),
        };
    }

    /// <summary>Checks a caller's sorting before it reaches <see cref="SortedBy"/>: a field or direction nobody declared is a 400.</summary>
    public static void EnsureDeclared(Sorting<ArticleSortField> sorting)
    {
        Ensure.That(sorting).IsNotNull();
        Ensure.That((int)sorting.Field, nameof(sorting)).Satisfies(field => Enum.IsDefined((ArticleSortField)field), $"An article list cannot be sorted by {sorting.Field}.");
        Ensure.That((int)sorting.Direction, nameof(sorting)).Satisfies(direction => Enum.IsDefined((SortDirection)direction), $"{sorting.Direction} is not a sort direction.");
    }

    private static IOrderedQueryable<Article> By<TKey>(IQueryable<Article> articles, Expression<Func<Article, TKey>> key, SortDirection direction) =>
        direction == SortDirection.Ascending
            ? articles.OrderBy(key).ThenBy(article => article.Id)
            : articles.OrderByDescending(key).ThenByDescending(article => article.Id);
}
