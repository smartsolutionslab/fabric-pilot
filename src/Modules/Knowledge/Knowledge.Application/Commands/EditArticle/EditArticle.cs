using FabricPilot.BuildingBlocks.Messaging;

namespace FabricPilot.Modules.Knowledge.Application.Commands.EditArticle;

/// <summary>Staff change an article's title and text, a draft or a published one.</summary>
public sealed record EditArticle(Guid ArticleId, string Title, string Body) : ICommand<Result<Unit, ApiError>>;
