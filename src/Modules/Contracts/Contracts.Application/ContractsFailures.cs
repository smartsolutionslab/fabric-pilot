using System.Net;
using FabricPilot.Modules.Contracts.Domain.Contracts;

namespace FabricPilot.Modules.Contracts.Application;

/// <summary>Everything a contracts use case refuses, each with its own code.</summary>
public static class ContractsFailures
{
    /// <summary>A contract rule the request broke: the domain's code, answered as 422.</summary>
    public static ApiError From(ContractError error) => error switch
    {
        InvalidProduct invalidProduct => ApiError.From(invalidProduct, HttpStatusCode.UnprocessableEntity),
        EndsBeforeItStarts endsBeforeItStarts => ApiError.From(endsBeforeItStarts, HttpStatusCode.UnprocessableEntity),
    };
}
