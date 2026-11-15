using FabricPilot.BuildingBlocks.Messaging;

namespace FabricPilot.Modules.Knowledge.Application.Commands.PublishArticle;

/// <summary>Staff publish a draft: every tenant's users can find and read it from then on.</summary>
public sealed record PublishArticle(Guid ArticleId) : ICommand<Result<Unit, ApiError>>;
