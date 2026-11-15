namespace FabricPilot.Modules.Billing.Domain.Invoices;

public sealed record InvalidCurrency(string Currency)
    : DomainError("billing.invalid-currency", $"'{Currency}' is not a currency code of three capital letters.");
