namespace FabricPilot.Modules.Billing.Application;

/// <summary>Everything a billing use case refuses, each with its own code.</summary>
public static class BillingFailures
{
    public static ApiError InvoiceNotFound(Guid invoice) => new InvoiceNotFound(invoice);
}
