using FabricPilot.BuildingBlocks.Messaging;

namespace FabricPilot.Modules.Knowledge.Application.Commands.WriteArticle;

/// <summary>Staff write a draft of the shared knowledge base; answers the new article's id.</summary>
public sealed record WriteArticle(string Title, string Body) : ICommand<Result<Guid, ApiError>>;
