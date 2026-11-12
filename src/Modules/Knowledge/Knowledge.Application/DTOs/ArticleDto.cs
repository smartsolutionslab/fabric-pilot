namespace FabricPilot.Modules.Knowledge.Application.DTOs;

/// <param name="NeedsIndexing">Published text the search does not find yet, because indexing it failed; publishing it again indexes it.</param>
public sealed record ArticleDto(Guid Id, string Title, string Body, string State, DateTimeOffset ChangedAt, DateTimeOffset? PublishedAt, bool NeedsIndexing);
