namespace FabricPilot.Modules.Billing.Domain.Invoices;

/// <summary>Every rule an invoice can break, each with its own code.</summary>
public static class InvoiceErrors
{
    public static InvoiceError InvalidNumber(int maximum) => new InvalidInvoiceNumber(maximum);

    public static InvoiceError InvalidCurrency(string currency) => new InvalidCurrency(currency);

    public static InvoiceError DueBeforeIssued(DateOnly issuedOn, DateOnly dueOn) => new DueBeforeIssued(issuedOn, dueOn);

    public static InvoiceError InvalidLineCount(int maximum) => new InvalidLineCount(maximum);

    public static InvoiceError InvalidLine(int maximumDescriptionLength, int maximumQuantity, decimal maximumUnitPrice) =>
        new InvalidInvoiceLine(maximumDescriptionLength, maximumQuantity, maximumUnitPrice);
}
