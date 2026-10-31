using FabricPilot.BuildingBlocks.Domain;
using FabricPilot.Modules.Billing.Domain.Invoices.Events;

namespace FabricPilot.Modules.Billing.Domain.Invoices;

public sealed class Invoice : AggregateRoot<InvoiceId>
{
    private Invoice(InvoiceId id, decimal amount)
        : base(id) => Amount = amount;

    public decimal Amount { get; }

    public bool IsPaid { get; private set; }

    public static Result<Invoice, ApiError> Create(decimal amount)
    {
        if (amount <= 0m)
        {
            return Failure(InvoiceFailures.AmountNotPositive(amount));
        }

        return Success(new Invoice(InvoiceId.New(), amount));
    }

    public Result<Invoice, ApiError> Pay()
    {
        IsPaid = true;
        Raise(new InvoicePaid(Id, Amount));
        return Success(this);
    }
}
