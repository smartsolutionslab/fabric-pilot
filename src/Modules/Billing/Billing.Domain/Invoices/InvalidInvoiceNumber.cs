namespace FabricPilot.Modules.Billing.Domain.Invoices;

public sealed record InvalidInvoiceNumber(int Maximum)
    : DomainError("billing.invalid-invoice-number", $"An invoice number is 1 to {Maximum} letters, digits or hyphens.");
