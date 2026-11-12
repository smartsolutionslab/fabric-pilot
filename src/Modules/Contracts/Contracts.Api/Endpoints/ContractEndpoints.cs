using FabricPilot.BuildingBlocks.Api;
using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Contracts.Application.DTOs;
using FabricPilot.Modules.Contracts.Application.Queries.ListContracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FabricPilot.Modules.Contracts.Api.Endpoints;

/// <summary>The caller's tenant's contracts.</summary>
public sealed class ContractEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        Ensure.That(endpoints).IsNotNull();

        endpoints.MapGroup("/contracts").WithTags("Contracts").MapGet(string.Empty, ListAsync);
    }

    /// <summary>The next contract to end first unless the caller names a field; a named field sorts ascending unless the caller says otherwise.</summary>
    private static async Task<Results<Ok<PagedResult<ContractSummaryDto>>, ProblemHttpResult>> ListAsync(
        Page? page,
        PageSize? pageSize,
        ContractSortField? sortBy,
        SortDirection? sortDirection,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var sorting = new Sorting<ContractSortField>(
            sortBy ?? ListContracts.EndingFirst.Field,
            sortDirection ?? ListContracts.EndingFirst.Direction);
        var listContracts = new ListContracts(page ?? Page.First, pageSize ?? PageSize.Default, sorting);
        return (await dispatcher.QueryAsync(listContracts, cancellationToken)).ToHttpResult();
    }
}
