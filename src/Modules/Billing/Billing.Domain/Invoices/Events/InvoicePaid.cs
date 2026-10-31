using FabricPilot.BuildingBlocks.Domain;

namespace FabricPilot.Modules.Billing.Domain.Invoices.Events;

public sealed record InvoicePaid(InvoiceId Invoice, decimal Amount) : IDomainEvent;
