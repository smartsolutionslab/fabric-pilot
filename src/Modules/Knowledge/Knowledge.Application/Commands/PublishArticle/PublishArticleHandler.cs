using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Contracts.Knowledge;
using FabricPilot.Modules.Knowledge.Domain.Articles;

namespace FabricPilot.Modules.Knowledge.Application.Commands.PublishArticle;

/// <summary>
/// Saves the article as published together with <see cref="ArticlePublishedV1"/>, then indexes its chunks, under the
/// article's lock. A draft's text never reaches the index every tenant searches. If indexing fails after the commit,
/// the request fails while the article stays published; its next edit indexes it again.
/// </summary>
public sealed class PublishArticleHandler(IArticleStore articles, IArticleIndex index, IArticleLocks locks, IKnowledgeUnitOfWork unitOfWork, TimeProvider time)
    : ICommandHandler<PublishArticle, Result<Unit, ApiError>>
{
    public async Task<Result<Unit, ApiError>> HandleAsync(PublishArticle command, CancellationToken cancellationToken)
    {
        Ensure.That(command).IsNotNull();

        var id = new ArticleId(command.ArticleId);
        await using var articleLock = await locks.AcquireAsync(id, cancellationToken);
        var found = await articles.FindAsync(id, cancellationToken);
        if (!found.HasValue)
        {
            return Failure(KnowledgeFailures.ArticleNotFound(command.ArticleId));
        }

        var article = found.Value;
        var published = article.Publish(time.GetUtcNow());
        if (published.IsFailure)
        {
            return Failure(KnowledgeFailures.From(published.Error));
        }

        await unitOfWork.PublishAsync(new ArticlePublishedV1(article.Id.Value, article.Title));
        await unitOfWork.CommitAsync(cancellationToken);
        await index.ReplaceAsync(article, cancellationToken);
        return Success(Unit.Value);
    }
}
