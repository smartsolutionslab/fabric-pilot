using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Knowledge.Domain.Articles;

namespace FabricPilot.Modules.Knowledge.Application.Commands.EditArticle;

/// <summary>
/// Saves the change first, a published article marked as needing indexing, then indexes it and saves it as indexed;
/// all under the article's lock: the index never holds text that was not saved, and two edits never interleave their
/// chunks. If indexing fails, publishing the article again indexes it.
/// </summary>
public sealed class EditArticleHandler(IArticleStore articles, IArticleIndex index, IArticleLocks locks, IKnowledgeUnitOfWork unitOfWork, TimeProvider time)
    : ICommandHandler<EditArticle, Result<Unit, ApiError>>
{
    public async Task<Result<Unit, ApiError>> HandleAsync(EditArticle command, CancellationToken cancellationToken)
    {
        Ensure.That(command).IsNotNull();

        var (articleId, title, body) = command;
        var id = new ArticleId(articleId);
        await using var articleLock = await locks.AcquireAsync(id, cancellationToken);
        var found = await articles.FindAsync(id, cancellationToken);
        if (!found.HasValue)
        {
            return Failure(KnowledgeFailures.ArticleNotFound(articleId));
        }

        var article = found.Value;
        var edited = article.Edit(title, body, time.GetUtcNow());
        if (edited.IsFailure)
        {
            return Failure(KnowledgeFailures.From(edited.Error));
        }

        await unitOfWork.CommitAsync(cancellationToken);
        if (article.NeedsIndexing)
        {
            await index.ReplaceAsync(article, cancellationToken);
            article.MarkIndexed();
            await unitOfWork.CommitAsync(cancellationToken);
        }

        return Success(Unit.Value);
    }
}
