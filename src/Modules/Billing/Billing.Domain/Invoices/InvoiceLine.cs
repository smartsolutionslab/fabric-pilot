namespace FabricPilot.Modules.Billing.Domain.Invoices;

/// <summary>What an invoice charges for: a description, how many and at what price each.</summary>
public sealed class InvoiceLine
{
    public const int MaximumDescriptionLength = 200;

    public const int MaximumQuantity = 9999;

    public const decimal MaximumUnitPrice = 9_999_999.99m;

    private InvoiceLine(string description, int quantity, decimal unitPrice)
    {
        Description = description;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    /// <summary>Where the line stands on its invoice, from 1; given when the invoice is issued.</summary>
    public int Position { get; private set; }

    public string Description { get; private set; }

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    /// <summary>The quantity times the unit price; exact, since the price is in whole cents.</summary>
    public decimal Amount => Quantity * UnitPrice;

    public static Result<InvoiceLine, InvoiceError> Describe(string description, int quantity, decimal unitPrice)
    {
        Ensure.That(description).IsNotNull();

        description = description.Trim();
        var valid = description.Length is > 0 and <= MaximumDescriptionLength
            && quantity is >= 1 and <= MaximumQuantity
            && unitPrice is >= 0 and <= MaximumUnitPrice
            && decimal.Round(unitPrice, 2) == unitPrice;
        if (!valid)
        {
            return Failure(InvoiceErrors.InvalidLine(MaximumDescriptionLength, MaximumQuantity, MaximumUnitPrice));
        }

        return Success(new InvoiceLine(description, quantity, unitPrice));
    }

    internal InvoiceLine CopyAt(int position) => new(Description, Quantity, UnitPrice) { Position = position };
}
