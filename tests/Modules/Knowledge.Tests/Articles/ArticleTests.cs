using FabricPilot.Modules.Knowledge.Domain.Articles;
using FabricPilot.Modules.Knowledge.Domain.Articles.Events;

namespace FabricPilot.Modules.Knowledge.Tests.Articles;

public class ArticleTests
{
    private static readonly DateTimeOffset WrittenAt = new(2026, 11, 12, 9, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset Later = WrittenAt.AddHours(2);

    [Fact]
    public void A_written_article_is_a_draft_with_its_trimmed_title_and_body()
    {
        var id = ArticleId.New();

        var result = Article.Write(id, "  Resetting a password  ", "  Open the sign-in page.  ", WrittenAt);

        result.IsSuccess.ShouldBeTrue();
        var article = result.Value;
        article.Id.ShouldBe(id);
        article.Title.ShouldBe("Resetting a password");
        article.Body.ShouldBe("Open the sign-in page.");
        article.State.ShouldBe(ArticleState.Draft);
        article.ChangedAt.ShouldBe(WrittenAt);
        article.PublishedAt.ShouldBeNull();
        article.DomainEvents.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void An_article_without_a_title_is_refused(string title)
    {
        var result = Article.Write(ArticleId.New(), title, "Body", WrittenAt);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<InvalidTitle>().Code.ShouldBe("knowledge.invalid-title");
    }

    [Fact]
    public void A_title_longer_than_the_maximum_is_refused_and_one_at_the_maximum_is_not()
    {
        Article.Write(ArticleId.New(), new string('t', Article.MaximumTitleLength + 1), "Body", WrittenAt).Error.ShouldBeOfType<InvalidTitle>();
        Article.Write(ArticleId.New(), new string('t', Article.MaximumTitleLength), "Body", WrittenAt).IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \n ")]
    public void An_article_without_a_body_is_refused(string body)
    {
        var result = Article.Write(ArticleId.New(), "Title", body, WrittenAt);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<InvalidBody>().Code.ShouldBe("knowledge.invalid-body");
    }

    [Fact]
    public void A_body_longer_than_the_maximum_is_refused_and_one_at_the_maximum_is_not()
    {
        Article.Write(ArticleId.New(), "Title", new string('b', Article.MaximumBodyLength + 1), WrittenAt).Error.ShouldBeOfType<InvalidBody>();
        Article.Write(ArticleId.New(), "Title", new string('b', Article.MaximumBodyLength), WrittenAt).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Editing_changes_the_title_body_and_time_of_the_last_change()
    {
        var article = Draft();

        var result = article.Edit(" New title ", " New body ", Later);

        result.IsSuccess.ShouldBeTrue();
        article.Title.ShouldBe("New title");
        article.Body.ShouldBe("New body");
        article.ChangedAt.ShouldBe(Later);
        article.State.ShouldBe(ArticleState.Draft);
    }

    [Fact]
    public void An_edit_that_breaks_a_rule_is_refused_and_changes_nothing()
    {
        var article = Draft();

        var result = article.Edit("New title", " ", Later);

        result.Error.ShouldBeOfType<InvalidBody>();
        article.Title.ShouldBe("Resetting a password");
        article.Body.ShouldBe("Open the sign-in page.");
        article.ChangedAt.ShouldBe(WrittenAt);
    }

    [Fact]
    public void A_published_article_stays_published_when_it_is_edited()
    {
        var article = Draft();
        article.Publish(Later);

        article.Edit("New title", "New body", Later.AddHours(1)).IsSuccess.ShouldBeTrue();

        article.State.ShouldBe(ArticleState.Published);
        article.PublishedAt.ShouldBe(Later);
    }

    [Fact]
    public void Publishing_a_draft_makes_it_published_and_raises_article_published()
    {
        var article = Draft();

        var result = article.Publish(Later);

        result.IsSuccess.ShouldBeTrue();
        article.State.ShouldBe(ArticleState.Published);
        article.PublishedAt.ShouldBe(Later);
        article.ChangedAt.ShouldBe(Later);
        article.DomainEvents.ShouldHaveSingleItem().ShouldBe(new ArticlePublished(article.Id));
    }

    [Fact]
    public void Publishing_a_published_article_again_is_refused_and_raises_nothing_more()
    {
        var article = Draft();
        article.Publish(Later);

        var result = article.Publish(Later.AddHours(1));

        result.Error.ShouldBeOfType<ArticleAlreadyPublished>().Code.ShouldBe("knowledge.article-already-published");
        article.PublishedAt.ShouldBe(Later);
        article.DomainEvents.Count.ShouldBe(1);
    }

    private static Article Draft() => Article.Write(ArticleId.New(), "Resetting a password", "Open the sign-in page.", WrittenAt).Value;
}
