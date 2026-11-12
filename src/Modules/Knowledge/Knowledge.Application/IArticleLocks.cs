using FabricPilot.Modules.Knowledge.Domain.Articles;

namespace FabricPilot.Modules.Knowledge.Application;

/// <summary>
/// One lock per article, across every instance of the host: a command that changes an article and its chunks holds it
/// from reading the article until the chunks are written, so two changes never interleave their writes to the index.
/// A change never waits for it: while another holds it, the change is refused.
/// </summary>
public interface IArticleLocks
{
    /// <summary>The article's lock, held until disposed; None at once if another change holds it.</summary>
    Task<Option<IAsyncDisposable>> TryAcquireAsync(ArticleId article, CancellationToken cancellationToken);
}
