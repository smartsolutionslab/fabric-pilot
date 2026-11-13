using System.Text.Json.Serialization;

namespace FabricPilot.Modules.Billing.Application.Queries.ListInvoices;

/// <summary>What the invoice list can be sorted by.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<InvoiceSortField>))]
public enum InvoiceSortField
{
    IssuedOn,
    DueOn,
    Number,
    Total,
}
