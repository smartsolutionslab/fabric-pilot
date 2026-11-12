using FabricPilot.BuildingBlocks.Api;
using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Contracts.Application;
using FabricPilot.Modules.Contracts.Application.DTOs;
using FabricPilot.Modules.Contracts.Application.Queries.ListContractsUpForRenewal;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FabricPilot.Modules.Contracts.Api.Endpoints;

/// <summary>Staff's list of every tenant's contracts up for renewal.</summary>
public sealed class ContractRenewalEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        Ensure.That(endpoints).IsNotNull();

        endpoints.MapGroup("/contracts").WithTags("Contracts")
            .MapGet("/up-for-renewal", ListAsync)
            .RequireAuthorization(ContractsPolicyNames.ReadAllTenants)
            .AcrossTenants();
    }

    private static async Task<Results<Ok<PagedResult<ContractSummaryDto>>, ProblemHttpResult>> ListAsync(
        Page? page,
        PageSize? pageSize,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var listContractsUpForRenewal = new ListContractsUpForRenewal(page ?? Page.First, pageSize ?? PageSize.Default);
        return (await dispatcher.QueryAsync(listContractsUpForRenewal, cancellationToken)).ToHttpResult();
    }
}
