namespace FabricPilot.Modules.Billing.Domain.Invoices;

/// <summary>The ways issuing an invoice fails. No use case issues one yet, so nothing maps them to an ApiError.</summary>
public union InvoiceError(InvalidInvoiceNumber, InvalidCurrency, DueBeforeIssued, InvalidLineCount, InvalidInvoiceLine);
