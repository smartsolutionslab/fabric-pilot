namespace FabricPilot.Evals;

/// <summary>
/// A ticket of the dataset: what a customer wrote, the articles the search finds for it (nearest first), and the titles of
/// those a good answer cites, none when no article answers it.
/// </summary>
public sealed record SupportTicket(string Subject, string Description, IReadOnlyList<KnowledgeArticle> Articles, IReadOnlyList<string> ExpectedSources);
