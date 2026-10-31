using System.Net;
using FabricPilot.Modules.Billing.Domain.Invoices;
using FabricPilot.Modules.Billing.Domain.Invoices.Events;

namespace FabricPilot.Modules.Billing.Tests.Invoices;

public class InvoiceTests
{
    [Fact]
    public void An_invoice_created_with_a_positive_amount_starts_unpaid()
    {
        var result = Invoice.Create(120.50m);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Amount.ShouldBe(120.50m);
        result.Value.IsPaid.ShouldBeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void An_invoice_with_an_amount_of_zero_or_less_is_rejected(double amount)
    {
        var result = Invoice.Create((decimal)amount);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("billing.invoice-amount-not-positive");
        result.Error.Status.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public void Paying_an_unpaid_invoice_marks_it_paid_and_raises_invoice_paid()
    {
        var invoice = Invoice.Create(10m).Value;

        var result = invoice.Pay();

        result.IsSuccess.ShouldBeTrue();
        invoice.IsPaid.ShouldBeTrue();
        invoice.DomainEvents.ShouldHaveSingleItem().ShouldBe(new InvoicePaid(invoice.Id, 10m));
    }

    [Fact]
    public void Paying_a_paid_invoice_is_an_error_not_a_second_payment()
    {
        var invoice = Invoice.Create(10m).Value;
        invoice.Pay();

        var result = invoice.Pay();

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("billing.invoice-already-paid");
        result.Error.Status.ShouldBe(HttpStatusCode.Conflict);
        invoice.IsPaid.ShouldBeTrue();
        invoice.DomainEvents.Count.ShouldBe(1);
    }
}
