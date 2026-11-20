using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Tickets.Application.DTOs;

namespace FabricPilot.Modules.Tickets.Application.Queries.ListSamples;

/// <summary>One page of the module's samples, by name.</summary>
public sealed record ListSamples(Page Page, PageSize PageSize) : IQuery<Result<PagedResult<TicketsSampleDto>, ApiError>>;
