namespace FabricPilot.Modules.Billing.Domain.Invoices;

public sealed record InvalidInvoiceLine(int MaximumDescriptionLength, int MaximumQuantity, decimal MaximumUnitPrice)
    : DomainError(
        "billing.invalid-invoice-line",
        $"An invoice line needs a description of 1 to {MaximumDescriptionLength} characters, a quantity of 1 to {MaximumQuantity} and a unit price of 0 to {MaximumUnitPrice} in whole cents.");
