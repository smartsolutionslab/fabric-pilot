namespace FabricPilot.Modules.Contracts.Domain.Contracts;

public sealed record EndsBeforeItStarts(DateOnly StartsOn, DateOnly EndsOn)
    : DomainError("contracts.ends-before-it-starts", $"A contract starting on {StartsOn:yyyy-MM-dd} cannot end on {EndsOn:yyyy-MM-dd}.");
