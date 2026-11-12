namespace FabricPilot.Modules.Knowledge.Application.DTOs;

public sealed record ArticleDto(Guid Id, string Title, string Body, string State, DateTimeOffset ChangedAt, DateTimeOffset? PublishedAt);
