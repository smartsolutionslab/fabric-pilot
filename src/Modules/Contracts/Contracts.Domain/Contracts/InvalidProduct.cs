namespace FabricPilot.Modules.Contracts.Domain.Contracts;

public sealed record InvalidProduct(int Maximum)
    : DomainError("contracts.invalid-product", $"A contract needs a product of 1 to {Maximum} characters.");
