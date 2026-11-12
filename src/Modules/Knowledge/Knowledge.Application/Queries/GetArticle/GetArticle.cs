using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Knowledge.Application.DTOs;

namespace FabricPilot.Modules.Knowledge.Application.Queries.GetArticle;

/// <summary>An article in either state, for the staff who edit it.</summary>
public sealed record GetArticle(Guid ArticleId) : IQuery<Result<ArticleDto, ApiError>>;
