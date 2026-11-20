using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Billing.Application.DTOs;

namespace FabricPilot.Modules.Billing.Application.Queries.ListSamples;

/// <summary>One page of the module's samples, by name.</summary>
public sealed record ListSamples(Page Page, PageSize PageSize) : IQuery<Result<PagedResult<BillingSampleDto>, ApiError>>;
