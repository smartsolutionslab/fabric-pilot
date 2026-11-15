namespace FabricPilot.Modules.Billing.Application.Queries.DownloadInvoicePdf;

/// <summary>An invoice laid out as a PDF, and the name to save it under.</summary>
public sealed record InvoicePdf(string FileName, byte[] Content);
