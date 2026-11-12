using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Knowledge.Domain.Articles;

namespace FabricPilot.Modules.Knowledge.Application.Commands.EditArticle;

/// <summary>
/// A published article is indexed again before the change is saved, so a search never finds text the article no longer
/// has for longer than the request takes; if saving fails, the index already holds the new text, which the next edit
/// replaces.
/// </summary>
public sealed class EditArticleHandler(IArticleStore articles, IArticleIndex index, IKnowledgeUnitOfWork unitOfWork, TimeProvider time)
    : ICommandHandler<EditArticle, Result<Unit, ApiError>>
{
    public async Task<Result<Unit, ApiError>> HandleAsync(EditArticle command, CancellationToken cancellationToken)
    {
        Ensure.That(command).IsNotNull();

        var (articleId, title, body) = command;
        var found = await articles.FindAsync(new ArticleId(articleId), cancellationToken);
        if (!found.HasValue)
        {
            return Failure(KnowledgeFailures.ArticleNotFound(articleId));
        }

        var article = found.Value;
        var previousChunkCount = ArticleChunks.Of(article.Title, article.Body).Count;
        var edited = article.Edit(title, body, time.GetUtcNow());
        if (edited.IsFailure)
        {
            return Failure(KnowledgeFailures.From(edited.Error));
        }

        if (article.State == ArticleState.Published)
        {
            await index.ReplaceAsync(article.Id, ArticleChunks.Of(article.Title, article.Body), previousChunkCount, cancellationToken);
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return Success(Unit.Value);
    }
}
