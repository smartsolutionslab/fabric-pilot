namespace FabricPilot.Modules.Knowledge.Application;

/// <summary>
/// The published articles' chunks in the vector index every tenant searches; implemented in the module's
/// Infrastructure project. Written only where articles are, by staff inside <c>WritesSharedRecords()</c>, and never
/// in the same transaction as the articles table: <see cref="ArticleIndexing"/> says in which order.
/// </summary>
public interface IArticleIndex
{
    /// <summary>Embeds the text and stores it under the key, replacing what the key held.</summary>
    Task IndexAsync(string key, string text, CancellationToken cancellationToken);

    /// <summary>Removes what the key holds, if anything.</summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken);
}
