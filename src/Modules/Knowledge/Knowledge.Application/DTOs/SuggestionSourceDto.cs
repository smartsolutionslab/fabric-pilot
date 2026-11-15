namespace FabricPilot.Modules.Knowledge.Application.DTOs;

/// <summary>An article a suggested answer is drafted from: its id, to link it, and its title.</summary>
public sealed record SuggestionSourceDto(Guid ArticleId, string Title);
