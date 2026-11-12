using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Knowledge.Application.DTOs;

namespace FabricPilot.Modules.Knowledge.Application.Queries.GetPublishedArticle;

/// <summary>A published article, as every tenant's users read it; a draft is not found.</summary>
public sealed record GetPublishedArticle(Guid ArticleId) : IQuery<Result<ArticleDto, ApiError>>;
