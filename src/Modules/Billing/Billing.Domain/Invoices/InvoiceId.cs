namespace FabricPilot.Modules.Billing.Domain.Invoices;

public sealed record InvoiceId(Guid Value)
{
    public static InvoiceId New() => new(Guid.CreateVersion7());
}
