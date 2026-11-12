using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Contracts.Knowledge;
using FabricPilot.Modules.Knowledge.Domain.Articles;

namespace FabricPilot.Modules.Knowledge.Application.Commands.PublishArticle;

/// <summary>
/// Indexes the article's chunks, then saves it as published together with <see cref="ArticlePublishedV1"/>. Indexing
/// comes first: if saving fails, the article stays a draft that searches never show, and publishing it again replaces
/// the same chunks.
/// </summary>
public sealed class PublishArticleHandler(IArticleStore articles, IArticleIndex index, IKnowledgeUnitOfWork unitOfWork, TimeProvider time)
    : ICommandHandler<PublishArticle, Result<Unit, ApiError>>
{
    public async Task<Result<Unit, ApiError>> HandleAsync(PublishArticle command, CancellationToken cancellationToken)
    {
        Ensure.That(command).IsNotNull();

        var found = await articles.FindAsync(new ArticleId(command.ArticleId), cancellationToken);
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

        await index.ReplaceAsync(article.Id, ArticleChunks.Of(article.Title, article.Body), 0, cancellationToken);
        await unitOfWork.PublishAsync(new ArticlePublishedV1(article.Id.Value, article.Title));
        await unitOfWork.CommitAsync(cancellationToken);
        return Success(Unit.Value);
    }
}
