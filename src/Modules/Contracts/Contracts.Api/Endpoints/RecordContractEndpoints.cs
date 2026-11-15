using FabricPilot.BuildingBlocks.Api;
using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Contracts.Application;
using FabricPilot.Modules.Contracts.Application.Commands.RecordContract;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FabricPilot.Modules.Contracts.Api.Endpoints;

/// <summary>Staff record a contract for the tenant the route names; that tenant's users then list it.</summary>
public sealed class RecordContractEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        Ensure.That(endpoints).IsNotNull();

        endpoints.MapGroup("/contracts").WithTags("Contracts")
            .MapPost("/{tenant}", RecordAsync)
            .RequireAuthorization(ContractsPolicyNames.WriteForTenants)
            .ForTenant();
    }

    /// <summary>
    /// The handler runs in the route's tenant (<c>ForTenant()</c>, which checked it), so the command does not carry it.
    /// No endpoint reads one contract, so the answer is the new id without a location.
    /// </summary>
    private static async Task<Results<Created<Guid>, ProblemHttpResult>> RecordAsync(RecordContract contract, IDispatcher dispatcher, CancellationToken cancellationToken)
    {
        Ensure.That(contract).IsNotNull();

        var result = await dispatcher.SendAsync(contract, cancellationToken);
        return result.ToHttpResult(id => TypedResults.Created((string?)null, id));
    }
}
