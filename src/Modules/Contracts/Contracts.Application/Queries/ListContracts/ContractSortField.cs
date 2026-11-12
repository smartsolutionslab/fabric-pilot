using System.Text.Json.Serialization;

namespace FabricPilot.Modules.Contracts.Application.Queries.ListContracts;

/// <summary>What the contract list can be sorted by.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ContractSortField>))]
public enum ContractSortField
{
    EndsOn,
    StartsOn,
    Product,
}
