namespace FabricPilot.Modules.Tickets.Application.Commands.RaiseTicket;

/// <summary>A file sent with a new ticket: what the caller says about it, and its content, which the caller disposes.</summary>
public sealed record NewAttachment(string FileName, string ContentType, long Size, Stream Content);
