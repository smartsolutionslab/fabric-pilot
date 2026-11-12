using System.Text.RegularExpressions;
using FabricPilot.BuildingBlocks.Domain;

namespace FabricPilot.Modules.Billing.Domain.Invoices;

/// <summary>What a customer owes for its lines, by when, and in which currency.</summary>
public sealed partial class Invoice : AggregateRoot<InvoiceId>
{
    /// <summary>An invoice number is safe in a file name: letters, digits and hyphens only.</summary>
    public const int MaximumNumberLength = 32;

    public const int MaximumLines = 100;

    private readonly List<InvoiceLine> lines = [];

    private Invoice(InvoiceId id, string number, DateOnly issuedOn, DateOnly dueOn, string currency)
        : base(id)
    {
        Number = number;
        IssuedOn = issuedOn;
        DueOn = dueOn;
        Currency = currency;
    }

    public string Number { get; private set; }

    public DateOnly IssuedOn { get; private set; }

    public DateOnly DueOn { get; private set; }

    /// <summary>The ISO 4217 code of the currency every amount on the invoice is in.</summary>
    public string Currency { get; private set; }

    /// <summary>The sum of the lines' amounts, worked out when the invoice is issued.</summary>
    public decimal Total { get; private set; }

    /// <summary>In the order they were issued in, however they were read back.</summary>
    public IReadOnlyList<InvoiceLine> Lines => [.. lines.OrderBy(line => line.Position)];

    public static Result<Invoice, InvoiceError> Issue(
        InvoiceId id,
        string number,
        DateOnly issuedOn,
        DateOnly dueOn,
        string currency,
        IReadOnlyList<InvoiceLine> lines)
    {
        Ensure.That(id).IsNotNull();
        Ensure.That(number).IsNotNull();
        Ensure.That(currency).IsNotNull();
        Ensure.That(lines).IsNotNull();

        number = number.Trim();
        if (!NumberPattern().IsMatch(number))
        {
            return Failure(InvoiceErrors.InvalidNumber(MaximumNumberLength));
        }

        if (!CurrencyPattern().IsMatch(currency))
        {
            return Failure(InvoiceErrors.InvalidCurrency(currency));
        }

        if (dueOn < issuedOn)
        {
            return Failure(InvoiceErrors.DueBeforeIssued(issuedOn, dueOn));
        }

        if (lines.Count is 0 or > MaximumLines)
        {
            return Failure(InvoiceErrors.InvalidLineCount(MaximumLines));
        }

        var invoice = new Invoice(id, number, issuedOn, dueOn, currency);
        foreach (var (index, line) in lines.Index())
        {
            line.PlaceAt(index + 1);
            invoice.lines.Add(line);
        }

        invoice.Total = lines.Sum(line => line.Amount);
        return Success(invoice);
    }

    [GeneratedRegex("^[A-Za-z0-9-]{1,32}\\z")]
    private static partial Regex NumberPattern();

    [GeneratedRegex("^[A-Z]{3}\\z")]
    private static partial Regex CurrencyPattern();
}
