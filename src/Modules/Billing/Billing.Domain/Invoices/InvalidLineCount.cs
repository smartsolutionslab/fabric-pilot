namespace FabricPilot.Modules.Billing.Domain.Invoices;

public sealed record InvalidLineCount(int Maximum)
    : DomainError("billing.invalid-line-count", $"An invoice needs 1 to {Maximum} lines.");
