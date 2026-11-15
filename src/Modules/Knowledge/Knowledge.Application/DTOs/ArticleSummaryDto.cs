namespace FabricPilot.Modules.Knowledge.Application.DTOs;

/// <summary>An article as a list shows it.</summary>
public sealed record ArticleSummaryDto(Guid Id, string Title, string State, DateTimeOffset ChangedAt, DateTimeOffset? PublishedAt);
