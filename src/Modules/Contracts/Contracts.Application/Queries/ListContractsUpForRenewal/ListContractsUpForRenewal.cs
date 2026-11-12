using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Contracts.Application.DTOs;

namespace FabricPilot.Modules.Contracts.Application.Queries.ListContractsUpForRenewal;

/// <summary>One page of the contracts up for renewal today, in every tenant the caller may read, the soonest to renew first.</summary>
public sealed record ListContractsUpForRenewal(Page Page, PageSize PageSize) : IQuery<Result<PagedResult<ContractSummaryDto>, ApiError>>;
