using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Documents.Application.DTOs;

namespace FabricPilot.Modules.Documents.Application.Queries.ListSamples;

/// <summary>One page of the module's samples, by name.</summary>
public sealed record ListSamples(Page Page, PageSize PageSize) : IQuery<Result<PagedResult<DocumentsSampleDto>, ApiError>>;
