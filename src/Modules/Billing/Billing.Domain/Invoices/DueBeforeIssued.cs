namespace FabricPilot.Modules.Billing.Domain.Invoices;

public sealed record DueBeforeIssued(DateOnly IssuedOn, DateOnly DueOn)
    : DomainError("billing.due-before-issued", $"An invoice issued on {IssuedOn:yyyy-MM-dd} cannot be due on {DueOn:yyyy-MM-dd}.");
