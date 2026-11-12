namespace FabricPilot.Modules.Contracts.Application.DTOs;

/// <summary>A contract as the list shows it; <see cref="RenewsOn"/> is empty for a contract that ends without renewing.</summary>
public sealed record ContractSummaryDto(Guid Id, string Product, DateOnly StartsOn, DateOnly EndsOn, DateOnly? RenewsOn);
