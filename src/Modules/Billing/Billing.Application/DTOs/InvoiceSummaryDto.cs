namespace FabricPilot.Modules.Billing.Application.DTOs;

/// <summary>An invoice as the list shows it; <see cref="Total"/> is in <see cref="Currency"/>.</summary>
public sealed record InvoiceSummaryDto(Guid Id, string Number, DateOnly IssuedOn, DateOnly DueOn, string Currency, decimal Total);
