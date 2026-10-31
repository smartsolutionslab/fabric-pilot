using System.Net;

namespace FabricPilot.Modules.Billing.Domain.Invoices;

public static class InvoiceFailures
{
    public static ApiError AmountNotPositive(decimal amount) => new InvoiceAmountNotPositive(amount);

    public static ApiError AlreadyPaid(InvoiceId id) => new InvoiceAlreadyPaid(id);
}

public sealed record InvoiceAmountNotPositive(decimal Amount)
    : ApiError("billing.invoice-amount-not-positive", $"An invoice amount must be greater than zero; got {Amount}.", HttpStatusCode.UnprocessableEntity);

public sealed record InvoiceAlreadyPaid(InvoiceId Invoice)
    : ApiError("billing.invoice-already-paid", $"Invoice {Invoice.Value} is already paid.", HttpStatusCode.Conflict);
