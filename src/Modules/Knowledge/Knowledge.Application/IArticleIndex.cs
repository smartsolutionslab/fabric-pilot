using FabricPilot.Modules.Knowledge.Domain.Articles;

namespace FabricPilot.Modules.Knowledge.Application;

/// <summary>
/// The published articles' chunks in the vector index every tenant searches; implemented in the module's
/// Infrastructure project. Written only where articles are, by staff inside <c>WritesSharedRecords()</c>.
/// </summary>
public interface IArticleIndex
{
    /// <summary>
    /// Indexes the article's chunks under <see cref="ArticleChunks.Key"/>, replacing what they held, and removes the
    /// chunks from <paramref name="chunks"/>' count up to <paramref name="previousChunkCount"/> that it no longer has.
    /// </summary>
    Task ReplaceAsync(ArticleId article, IReadOnlyList<string> chunks, int previousChunkCount, CancellationToken cancellationToken);
}
