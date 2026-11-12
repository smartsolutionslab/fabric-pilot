using FabricPilot.Modules.Knowledge.Application;
using FabricPilot.Modules.Knowledge.Domain.Articles;

namespace FabricPilot.Modules.Knowledge.Tests;

public class ArticleChunksTests
{
    [Fact]
    public void A_short_article_is_one_chunk_that_starts_with_its_title()
    {
        var chunks = ArticleChunks.Of("Resetting a password", "Open the sign-in page.\n\nChoose Forgot password.");

        chunks.ShouldBe(["Resetting a password\n\nOpen the sign-in page.\n\nChoose Forgot password."]);
    }

    [Fact]
    public void Paragraphs_are_packed_into_chunks_of_at_most_the_maximum_and_never_split_while_they_fit()
    {
        var first = new string('a', 600);
        var second = new string('b', 600);
        var third = new string('c', 300);

        var chunks = ArticleChunks.Of("Title", $"{first}\n\n{second}\r\n\r\n{third}");

        chunks.ShouldBe([$"Title\n\n{first}", $"Title\n\n{second}\n\n{third}"]);
    }

    [Fact]
    public void A_paragraph_longer_than_the_maximum_is_cut_at_white_space()
    {
        var words = string.Join(' ', Enumerable.Repeat("word", 450));

        var chunks = ArticleChunks.Of("Title", words);

        chunks.Count.ShouldBe(3);
        chunks.ShouldAllBe(chunk => chunk.StartsWith("Title\n\n", StringComparison.Ordinal));
        chunks.Select(chunk => chunk["Title\n\n".Length..].Length).ShouldAllBe(length => length <= ArticleChunks.MaximumLength);
        string.Join(' ', chunks.Select(chunk => chunk["Title\n\n".Length..])).ShouldBe(words);
    }

    [Fact]
    public void A_word_longer_than_the_maximum_is_cut_where_the_maximum_falls()
    {
        var word = new string('x', ArticleChunks.MaximumLength + 10);

        var chunks = ArticleChunks.Of("Title", word);

        chunks.ShouldBe([$"Title\n\n{word[..ArticleChunks.MaximumLength]}", $"Title\n\n{word[ArticleChunks.MaximumLength..]}"]);
    }

    [Fact]
    public void The_longest_article_has_no_more_chunks_than_the_maximum_count()
    {
        // Paragraphs that just miss packing together: each chunk holds one of them.
        var pair = $"{new string('a', ArticleChunks.MaximumLength - 2)}\n\nb\n\n";
        var body = string.Concat(Enumerable.Repeat(pair, (Article.MaximumBodyLength / pair.Length) + 1))[..Article.MaximumBodyLength];

        ArticleChunks.Of("Title", body).Count.ShouldBeLessThanOrEqualTo(ArticleChunks.MaximumCount);
    }

    [Fact]
    public void A_chunks_key_names_its_article_and_its_position()
    {
        var id = new ArticleId(Guid.Parse("0193c0de-0000-7000-8000-000000000001"));

        ArticleChunks.Key(id, 0).ShouldBe("knowledge/articles/0193c0de-0000-7000-8000-000000000001/0");
        ArticleChunks.Key(id, 2).ShouldBe("knowledge/articles/0193c0de-0000-7000-8000-000000000001/2");
    }

    [Fact]
    public void A_chunks_key_names_its_article()
    {
        var article = ArticleId.New();

        ArticleChunks.ArticleOf(ArticleChunks.Key(article, 3)).ShouldBe(Option<ArticleId>.Some(article));
    }

    [Theory]
    [InlineData("")]
    [InlineData("knowledge/articles/not-a-guid/0")]
    [InlineData("knowledge/articles/0193c0de-0000-7000-8000-000000000011")]
    [InlineData("knowledge/articles/0193c0de-0000-7000-8000-000000000011/x")]
    [InlineData("tickets/articles/0193c0de-0000-7000-8000-000000000011/0")]
    [InlineData("knowledge/articles/0193c0de-0000-7000-8000-000000000011/0/1")]
    public void A_key_that_is_not_a_chunks_names_no_article(string key) =>
        ArticleChunks.ArticleOf(key).HasValue.ShouldBeFalse();
}
