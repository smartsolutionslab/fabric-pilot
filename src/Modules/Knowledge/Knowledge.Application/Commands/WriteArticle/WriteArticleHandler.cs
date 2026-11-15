using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Knowledge.Domain.Articles;

namespace FabricPilot.Modules.Knowledge.Application.Commands.WriteArticle;

/// <summary>A draft is neither indexed nor announced: nobody but staff sees it until it is published.</summary>
public sealed class WriteArticleHandler(IArticleStore articles, IKnowledgeUnitOfWork unitOfWork, TimeProvider time)
    : ICommandHandler<WriteArticle, Result<Guid, ApiError>>
{
    public async Task<Result<Guid, ApiError>> HandleAsync(WriteArticle command, CancellationToken cancellationToken)
    {
        Ensure.That(command).IsNotNull();

        var (title, body) = command;
        var written = Article.Write(ArticleId.New(), title, body, time.GetUtcNow());
        if (written.IsFailure)
        {
            return Failure(KnowledgeFailures.From(written.Error));
        }

        articles.Add(written.Value);
        await unitOfWork.CommitAsync(cancellationToken);
        return Success(written.Value.Id.Value);
    }
}
