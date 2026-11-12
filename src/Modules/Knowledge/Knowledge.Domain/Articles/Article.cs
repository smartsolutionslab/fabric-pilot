using FabricPilot.BuildingBlocks.Domain;
using FabricPilot.Modules.Knowledge.Domain.Articles.Events;

namespace FabricPilot.Modules.Knowledge.Domain.Articles;

/// <summary>
/// An article of the knowledge base the operator's staff write and every tenant reads: a draft until it is published,
/// and editable in either state.
/// </summary>
public sealed class Article : AggregateRoot<ArticleId>
{
    public const int MaximumTitleLength = 200;

    public const int MaximumBodyLength = 20_000;

    private Article(ArticleId id, string title, string body, DateTimeOffset changedAt)
        : base(id)
    {
        Title = title;
        Body = body;
        State = ArticleState.Draft;
        ChangedAt = changedAt;
    }

    public string Title { get; private set; }

    public string Body { get; private set; }

    public ArticleState State { get; private set; }

    /// <summary>When it was last written, edited or published.</summary>
    public DateTimeOffset ChangedAt { get; private set; }

    /// <summary>When it was published; null while it is a draft.</summary>
    public DateTimeOffset? PublishedAt { get; private set; }

    public static Result<Article, ArticleError> Write(ArticleId id, string title, string body, DateTimeOffset writtenAt)
    {
        Ensure.That(id).IsNotNull();

        var checkedText = Checked(title, body);
        if (checkedText.IsFailure)
        {
            return Failure(checkedText.Error);
        }

        var (trimmedTitle, trimmedBody) = checkedText.Value;
        return Success(new Article(id, trimmedTitle, trimmedBody, writtenAt));
    }

    public Result<Unit, ArticleError> Edit(string title, string body, DateTimeOffset editedAt)
    {
        var checkedText = Checked(title, body);
        if (checkedText.IsFailure)
        {
            return Failure(checkedText.Error);
        }

        (Title, Body) = checkedText.Value;
        ChangedAt = editedAt;
        return Success(Unit.Value);
    }

    public Result<Unit, ArticleError> Publish(DateTimeOffset publishedAt)
    {
        if (State == ArticleState.Published)
        {
            return Failure(ArticleErrors.AlreadyPublished(Id));
        }

        State = ArticleState.Published;
        PublishedAt = publishedAt;
        ChangedAt = publishedAt;
        Raise(new ArticlePublished(Id));
        return Success(Unit.Value);
    }

    private static Result<(string Title, string Body), ArticleError> Checked(string title, string body)
    {
        Ensure.That(title).IsNotNull();
        Ensure.That(body).IsNotNull();

        var trimmedTitle = title.Trim();
        if (trimmedTitle.Length == 0 || trimmedTitle.Length > MaximumTitleLength)
        {
            return Failure(ArticleErrors.InvalidTitle(MaximumTitleLength));
        }

        var trimmedBody = body.Trim();
        if (trimmedBody.Length == 0 || trimmedBody.Length > MaximumBodyLength)
        {
            return Failure(ArticleErrors.InvalidBody(MaximumBodyLength));
        }

        return Success((trimmedTitle, trimmedBody));
    }
}
