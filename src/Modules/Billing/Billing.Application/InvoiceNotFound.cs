using System.Net;

namespace FabricPilot.Modules.Billing.Application;

/// <summary>Also what another tenant's invoice answers: its existence is not the caller's to know.</summary>
public sealed record InvoiceNotFound(Guid Invoice)
    : ApiError("billing.invoice-not-found", $"There is no invoice {Invoice}.", HttpStatusCode.NotFound);
