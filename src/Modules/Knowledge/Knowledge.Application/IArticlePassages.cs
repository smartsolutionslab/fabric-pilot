namespace FabricPilot.Modules.Knowledge.Application;

/// <summary>
/// Searches the published articles' chunks in the shared index every tenant reads; implemented in the module's
/// Infrastructure project. Every search sends the question to the embeddings provider.
/// </summary>
public interface IArticlePassages
{
    /// <summary>At most <paramref name="count"/> passages of articles that are published now, nearest to the question first.</summary>
    Task<IReadOnlyList<ArticlePassage>> NearestAsync(string question, int count, CancellationToken cancellationToken);
}
