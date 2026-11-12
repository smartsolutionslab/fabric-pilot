using System.Linq.Expressions;
using FabricPilot.Modules.Contracts.Domain.Contracts;

namespace FabricPilot.Modules.Contracts.Application.DTOs;

public static class ContractMappings
{
    /// <summary>For an EF <c>Select</c>: the renewal date is the one the domain stored, never computed here.</summary>
    public static Expression<Func<Contract, ContractSummaryDto>> ToContractSummaryDtoExpression() =>
        contract => new ContractSummaryDto(contract.Id.Value, contract.Product, contract.StartsOn, contract.EndsOn, contract.RenewsOn);
}
