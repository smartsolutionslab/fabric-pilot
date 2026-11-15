using System.Net;
using FabricPilot.Modules.Knowledge.Application;
using FabricPilot.Modules.Knowledge.Application.Queries.GetArticle;
using FabricPilot.Modules.Knowledge.Application.Queries.GetPublishedArticle;
using FabricPilot.Modules.Knowledge.Application.Queries.ListArticles;
using FabricPilot.Modules.Knowledge.Application.Queries.SearchArticles;
using FabricPilot.Modules.Knowledge.Domain.Articles;
using SortDirection = FabricPilot.Kernel.SortDirection;

namespace FabricPilot.Modules.Knowledge.Tests.Queries;

public class ArticleQueriesTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 10, 0, 0, TimeSpan.Zero);

    private readonly InMemoryArticleStore articles = new();

    [Fact]
    public async Task A_search_finds_published_articles_whose_title_or_text_contains_the_term_in_any_case()
    {
        var byTitle = Published("Printer offline", "Check the cable.", Now);
        var byText = Published("Network", "If the PRINTER does not answer, restart it.", Now.AddMinutes(1));
        Published("Passwords", "Use the reset link.", Now.AddMinutes(2));
        Draft("Printer drivers", "Not ready yet.", Now.AddMinutes(3));

        var result = await Search("printer");

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Select(item => item.Id).ShouldBe([byText.Id.Value, byTitle.Id.Value]);
        result.Value.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task A_search_without_a_term_lists_every_published_article_and_no_draft()
    {
        var published = Published("Printer offline", "Check the cable.", Now);
        Draft("Printer drivers", "Not ready yet.", Now.AddMinutes(1));

        var result = await Search(string.Empty);

        result.Value.Items.ShouldHaveSingleItem().Id.ShouldBe(published.Id.Value);
        result.Value.Items[0].State.ShouldBe("Published");
    }

    [Fact]
    public async Task A_search_pages_and_sorts_as_asked()
    {
        Published("Bravo", "Text.", Now);
        Published("Alpha", "Text.", Now.AddMinutes(1));
        Published("Charlie", "Text.", Now.AddMinutes(2));
        PageSize.TryFrom(2, out var pageSize).ShouldBeTrue();
        Page.TryFrom(2, out var second).ShouldBeTrue();

        var result = await new SearchArticlesHandler(articles).HandleAsync(
            new SearchArticles("text", second, pageSize, new Sorting<ArticleSortField>(ArticleSortField.Title, SortDirection.Ascending)),
            TestContext.Current.CancellationToken);

        result.Value.Items.Select(item => item.Title).ShouldBe(["Charlie"]);
        result.Value.TotalCount.ShouldBe(3);
        result.Value.Page.ShouldBe(2);
    }

    [Fact]
    public async Task The_default_order_is_the_latest_change_first()
    {
        SearchArticles.LatestFirst.ShouldBe(new Sorting<ArticleSortField>(ArticleSortField.ChangedAt, SortDirection.Descending));
        ListArticles.LatestFirst.ShouldBe(new Sorting<ArticleSortField>(ArticleSortField.ChangedAt, SortDirection.Descending));

        Published("Older", "Text.", Now);
        Published("Newer", "Text.", Now.AddMinutes(1));

        var result = await Search(string.Empty);

        result.Value.Items.Select(item => item.Title).ShouldBe(["Newer", "Older"]);
    }

    [Fact]
    public async Task A_customer_reads_a_published_article()
    {
        var article = Published("Printer offline", "Check the cable.", Now);

        var result = await new GetPublishedArticleHandler(articles).HandleAsync(new GetPublishedArticle(article.Id.Value), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(article.Id.Value);
        result.Value.Title.ShouldBe("Printer offline");
        result.Value.Body.ShouldBe("Check the cable.");
        result.Value.State.ShouldBe("Published");
        result.Value.PublishedAt.ShouldBe(Now);
    }

    [Fact]
    public async Task A_draft_is_not_found_for_a_customer_as_if_it_did_not_exist()
    {
        var draft = Draft("Printer drivers", "Not ready yet.", Now);

        var result = await new GetPublishedArticleHandler(articles).HandleAsync(new GetPublishedArticle(draft.Id.Value), TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe("knowledge.article-not-found");
        result.Error.Status.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Staff_list_every_article_drafts_included_with_its_state()
    {
        var published = Published("Printer offline", "Check the cable.", Now);
        var draft = Draft("Printer drivers", "Not ready yet.", Now.AddMinutes(1));

        var result = await new ListArticlesHandler(articles).HandleAsync(
            new ListArticles(Page.First, PageSize.Default, ListArticles.LatestFirst),
            TestContext.Current.CancellationToken);

        result.Value.Items.Select(item => (item.Id, item.State)).ShouldBe([(draft.Id.Value, "Draft"), (published.Id.Value, "Published")]);
    }

    [Fact]
    public async Task Staff_read_a_draft_and_a_missing_article_is_404()
    {
        var draft = Draft("Printer drivers", "Not ready yet.", Now);
        var handler = new GetArticleHandler(articles);

        var found = await handler.HandleAsync(new GetArticle(draft.Id.Value), TestContext.Current.CancellationToken);
        var missing = await handler.HandleAsync(new GetArticle(Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        found.Value.State.ShouldBe("Draft");
        found.Value.Body.ShouldBe("Not ready yet.");
        found.Value.PublishedAt.ShouldBeNull();
        missing.Error.Code.ShouldBe("knowledge.article-not-found");
    }

    [Fact]
    public async Task A_sort_field_the_query_does_not_declare_is_refused()
    {
        var handle = () => new SearchArticlesHandler(articles).HandleAsync(
            new SearchArticles(string.Empty, Page.First, PageSize.Default, new Sorting<ArticleSortField>((ArticleSortField)99, SortDirection.Ascending)),
            TestContext.Current.CancellationToken);

        await Should.ThrowAsync<GuardException>(handle);
    }

    private Task<Result<PagedResult<Application.DTOs.ArticleSummaryDto>, ApiError>> Search(string term) =>
        new SearchArticlesHandler(articles).HandleAsync(new SearchArticles(term, Page.First, PageSize.Default, SearchArticles.LatestFirst), TestContext.Current.CancellationToken);

    private Article Published(string title, string body, DateTimeOffset at)
    {
        var article = Article.Write(ArticleId.New(), title, body, at).Value;
        article.Publish(at);
        articles.Add(article);
        articles.Commit();
        return article;
    }

    private Article Draft(string title, string body, DateTimeOffset at)
    {
        var article = Article.Write(ArticleId.New(), title, body, at).Value;
        articles.Add(article);
        articles.Commit();
        return article;
    }
}
