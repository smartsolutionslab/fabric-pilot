using FabricPilot.Modules.Knowledge.Domain.Articles;

namespace FabricPilot.Modules.Knowledge.Application;

/// <summary>
/// One lock per article, across every instance of the host: a command that changes an article and its chunks holds it
/// from reading the article until the chunks are written, so two changes never interleave their writes to the index.
/// </summary>
public interface IArticleLocks
{
    /// <summary>Waits for the article's lock and holds it until the result is disposed.</summary>
    Task<IAsyncDisposable> AcquireAsync(ArticleId article, CancellationToken cancellationToken);
}
