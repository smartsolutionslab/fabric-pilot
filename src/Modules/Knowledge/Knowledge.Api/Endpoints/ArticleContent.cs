namespace FabricPilot.Modules.Knowledge.Api.Endpoints;

/// <summary>What staff write into an article, new or edited.</summary>
public sealed record ArticleContent(string Title, string Body);
