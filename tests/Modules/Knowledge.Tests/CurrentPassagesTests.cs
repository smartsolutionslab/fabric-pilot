using FabricPilot.Modules.Knowledge.Application;
using FabricPilot.Modules.Knowledge.Domain.Articles;

namespace FabricPilot.Modules.Knowledge.Tests;

/// <summary>
/// The passages a suggestion is drafted from are chunks of the articles as they stand now: published, indexed, and
/// still holding the chunk's text. The search reaches further when the nearest chunks are left out.
/// </summary>
public class CurrentPassagesTests
{
    private static readonly DateTimeOffset WrittenAt = new(2026, 11, 13, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task A_chunk_of_a_published_indexed_article_is_a_passage_under_its_current_title()
    {
        var article = Indexed("Printer error 42", "Restart the spooler.");
        var search = new FakeSearch([Chunk(article, "Printer error 42", "Restart the spooler.")]);

        var passages = await CurrentPassages.NearestAsync(search.SearchAsync, Articles(article), 5, TestContext.Current.CancellationToken);

        var passage = passages.ShouldHaveSingleItem();
        passage.Article.ShouldBe(article.Id);
        passage.Title.ShouldBe("Printer error 42");
        passage.Text.ShouldBe("Printer error 42\n\nRestart the spooler.");
    }

    [Fact]
    public async Task A_chunk_whose_text_the_article_no_longer_holds_is_left_out_even_once_it_is_indexed_again()
    {
        var article = Indexed("Router", "Previous guidance: restart the router.");
        var stale = Chunk(article, "Router", "Previous guidance: restart the router.");
        article.Edit("Router", "Current guidance: call support.", WrittenAt.AddMinutes(1)).IsSuccess.ShouldBeTrue();
        article.MarkIndexed();
        var search = new FakeSearch([stale]);

        var passages = await CurrentPassages.NearestAsync(search.SearchAsync, Articles(article), 5, TestContext.Current.CancellationToken);

        passages.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_chunk_of_an_article_awaiting_indexing_or_not_published_is_left_out()
    {
        var awaiting = Article.Write(ArticleId.New(), "Awaiting", "Being indexed.", WrittenAt).Value;
        awaiting.Publish(WrittenAt).IsSuccess.ShouldBeTrue();
        var draft = Article.Write(ArticleId.New(), "Draft", "Not published.", WrittenAt).Value;
        var search = new FakeSearch([Chunk(awaiting, "Awaiting", "Being indexed."), Chunk(draft, "Draft", "Not published.")]);

        var passages = await CurrentPassages.NearestAsync(search.SearchAsync, Articles(awaiting, draft), 5, TestContext.Current.CancellationToken);

        passages.ShouldBeEmpty();
    }

    [Fact]
    public async Task When_the_nearest_chunks_are_left_out_the_search_reaches_further_until_it_has_the_count()
    {
        List<Article> awaiting = [.. Enumerable.Range(0, 8).Select(number => Awaiting($"Awaiting {number}", "Being indexed."))];
        List<Article> current = [.. Enumerable.Range(0, 6).Select(number => Indexed($"Current {number}", "Answers it."))];
        var search = new FakeSearch([
            .. awaiting.Select((article, number) => Chunk(article, $"Awaiting {number}", "Being indexed.")),
            .. current.Select((article, number) => Chunk(article, $"Current {number}", "Answers it.")),
        ]);

        var passages = await CurrentPassages.NearestAsync(search.SearchAsync, Articles([.. awaiting, .. current]), 5, TestContext.Current.CancellationToken);

        passages.Select(passage => passage.Article).ShouldBe(current.Take(5).Select(article => article.Id));
        search.Requested.ShouldBe([5, 10, 20]);
    }

    [Fact]
    public async Task When_the_index_holds_fewer_chunks_than_asked_the_passages_found_are_all_there_are()
    {
        var awaiting = Awaiting("Awaiting", "Being indexed.");
        var current = Indexed("Current", "Answers it.");
        var search = new FakeSearch([Chunk(awaiting, "Awaiting", "Being indexed."), Chunk(current, "Current", "Answers it.")]);

        var passages = await CurrentPassages.NearestAsync(search.SearchAsync, Articles(awaiting, current), 5, TestContext.Current.CancellationToken);

        passages.ShouldHaveSingleItem().Article.ShouldBe(current.Id);
        search.Requested.ShouldBe([5]);
    }

    private static Article Indexed(string title, string body)
    {
        var article = Awaiting(title, body);
        article.MarkIndexed();
        return article;
    }

    private static Article Awaiting(string title, string body)
    {
        var article = Article.Write(ArticleId.New(), title, body, WrittenAt).Value;
        article.Publish(WrittenAt).IsSuccess.ShouldBeTrue();
        return article;
    }

    private static IndexedChunk Chunk(Article article, string title, string body) =>
        new(ArticleChunks.Key(article.Id, 0), ArticleChunks.Of(title, body).ShouldHaveSingleItem());

    /// <summary>The articles table as it stands: every article the test holds, whatever its state, looked up by id.</summary>
    private static Func<IReadOnlyCollection<ArticleId>, CancellationToken, Task<IReadOnlyList<Article>>> Articles(params Article[] articles) =>
        (ids, _) => Task.FromResult<IReadOnlyList<Article>>([.. articles.Where(article => ids.Contains(article.Id))]);

    /// <summary>The shared index: its chunks nearest first, and every count it was asked for.</summary>
    private sealed class FakeSearch(IReadOnlyList<IndexedChunk> chunks)
    {
        public List<int> Requested { get; } = [];

        public Task<IReadOnlyList<IndexedChunk>> SearchAsync(int count, CancellationToken cancellationToken)
        {
            Requested.Add(count);
            return Task.FromResult<IReadOnlyList<IndexedChunk>>([.. chunks.Take(count)]);
        }
    }
}
