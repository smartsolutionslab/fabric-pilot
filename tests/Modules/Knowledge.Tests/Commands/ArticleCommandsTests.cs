using System.Net;
using FabricPilot.Contracts.Knowledge;
using FabricPilot.Modules.Knowledge.Application;
using FabricPilot.Modules.Knowledge.Application.Commands.EditArticle;
using FabricPilot.Modules.Knowledge.Application.Commands.PublishArticle;
using FabricPilot.Modules.Knowledge.Application.Commands.WriteArticle;
using FabricPilot.Modules.Knowledge.Domain.Articles;
using Microsoft.Extensions.Time.Testing;

namespace FabricPilot.Modules.Knowledge.Tests.Commands;

public class ArticleCommandsTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 10, 0, 0, TimeSpan.Zero);

    private readonly InMemoryArticleStore articles = new();
    private readonly InMemoryArticleIndex index = new();
    private readonly FakeTimeProvider time = new(Now);
    private readonly RecordingUnitOfWork unitOfWork;

    public ArticleCommandsTests()
    {
        unitOfWork = new RecordingUnitOfWork(articles);
    }

    [Fact]
    public async Task A_written_article_is_saved_as_a_draft_and_neither_indexed_nor_announced()
    {
        var result = await Write("Resetting a password", "Open the sign-in page.");

        result.IsSuccess.ShouldBeTrue();
        var article = articles.Saved.ShouldHaveSingleItem();
        article.Id.Value.ShouldBe(result.Value);
        article.State.ShouldBe(ArticleState.Draft);
        article.ChangedAt.ShouldBe(Now);
        index.Chunks.ShouldBeEmpty();
        unitOfWork.Published.ShouldBeEmpty();
    }

    [Fact]
    public async Task An_article_that_breaks_a_rule_is_refused_with_the_domains_code_as_422_and_nothing_is_saved()
    {
        var result = await Write(" ", "Body");

        result.Error.Code.ShouldBe("knowledge.invalid-title");
        result.Error.Status.ShouldBe(HttpStatusCode.UnprocessableEntity);
        articles.Saved.ShouldBeEmpty();
        unitOfWork.Commits.ShouldBe(0);
    }

    [Fact]
    public async Task Publishing_indexes_the_articles_chunks_and_announces_it_in_the_same_commit()
    {
        var id = (await Write("Resetting a password", "Open the sign-in page.\n\nChoose Forgot password.")).Value;
        time.Advance(TimeSpan.FromHours(1));

        var result = await Publish(id);

        result.IsSuccess.ShouldBeTrue();
        var article = articles.Saved.ShouldHaveSingleItem();
        article.State.ShouldBe(ArticleState.Published);
        article.PublishedAt.ShouldBe(Now.AddHours(1));
        index.Chunks.ShouldBe(new Dictionary<string, string>
        {
            [ArticleChunks.Key(article.Id, 0)] = "Resetting a password\n\nOpen the sign-in page.\n\nChoose Forgot password.",
        });
        unitOfWork.Published.ShouldHaveSingleItem().ShouldBe(new ArticlePublishedV1(id, "Resetting a password"));
        unitOfWork.Commits.ShouldBe(2);
    }

    [Fact]
    public async Task Publishing_an_article_twice_is_refused_as_409_and_announced_once()
    {
        var id = (await Write("Title", "Body")).Value;
        await Publish(id);

        var result = await Publish(id);

        result.Error.Code.ShouldBe("knowledge.article-already-published");
        result.Error.Status.ShouldBe(HttpStatusCode.Conflict);
        unitOfWork.Published.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Publishing_an_article_that_does_not_exist_is_404_and_indexes_nothing()
    {
        var missing = Guid.CreateVersion7();

        var result = await Publish(missing);

        result.Error.Code.ShouldBe("knowledge.article-not-found");
        result.Error.Status.ShouldBe(HttpStatusCode.NotFound);
        index.Chunks.ShouldBeEmpty();
        unitOfWork.Published.ShouldBeEmpty();
    }

    [Fact]
    public async Task Editing_a_draft_saves_the_change_and_indexes_nothing()
    {
        var id = (await Write("Title", "Body")).Value;
        time.Advance(TimeSpan.FromMinutes(5));

        var result = await Edit(id, "New title", "New body");

        result.IsSuccess.ShouldBeTrue();
        var article = articles.Saved.ShouldHaveSingleItem();
        article.Title.ShouldBe("New title");
        article.Body.ShouldBe("New body");
        article.ChangedAt.ShouldBe(Now.AddMinutes(5));
        index.Chunks.ShouldBeEmpty();
    }

    [Fact]
    public async Task Editing_a_published_article_indexes_it_again_and_removes_the_chunks_it_no_longer_has()
    {
        var long1 = new string('a', 900);
        var long2 = new string('b', 900);
        var id = (await Write("Title", $"{long1}\n\n{long2}")).Value;
        await Publish(id);
        index.Chunks.Count.ShouldBe(2);

        var result = await Edit(id, "Shorter", "One paragraph now.");

        result.IsSuccess.ShouldBeTrue();
        var article = articles.Saved.ShouldHaveSingleItem();
        index.Chunks.ShouldBe(new Dictionary<string, string>
        {
            [ArticleChunks.Key(article.Id, 0)] = "Shorter\n\nOne paragraph now.",
        });
        unitOfWork.Published.Count.ShouldBe(1);
    }

    [Fact]
    public async Task An_edit_that_breaks_a_rule_is_refused_and_leaves_the_index_as_it_was()
    {
        var id = (await Write("Title", "Body")).Value;
        await Publish(id);
        var indexed = new Dictionary<string, string>(index.Chunks);

        var result = await Edit(id, "Title", " ");

        result.Error.Code.ShouldBe("knowledge.invalid-body");
        result.Error.Status.ShouldBe(HttpStatusCode.UnprocessableEntity);
        index.Chunks.ShouldBe(indexed);
    }

    [Fact]
    public async Task Editing_an_article_that_does_not_exist_is_404()
    {
        var result = await Edit(Guid.CreateVersion7(), "Title", "Body");

        result.Error.Code.ShouldBe("knowledge.article-not-found");
        result.Error.Status.ShouldBe(HttpStatusCode.NotFound);
    }

    private Task<Result<Guid, ApiError>> Write(string title, string body) =>
        new WriteArticleHandler(articles, unitOfWork, time).HandleAsync(new WriteArticle(title, body), TestContext.Current.CancellationToken);

    private Task<Result<Unit, ApiError>> Publish(Guid id) =>
        new PublishArticleHandler(articles, index, unitOfWork, time).HandleAsync(new PublishArticle(id), TestContext.Current.CancellationToken);

    private Task<Result<Unit, ApiError>> Edit(Guid id, string title, string body) =>
        new EditArticleHandler(articles, index, unitOfWork, time).HandleAsync(new EditArticle(id, title, body), TestContext.Current.CancellationToken);
}
