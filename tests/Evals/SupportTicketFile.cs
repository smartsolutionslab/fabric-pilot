namespace FabricPilot.Evals;

/// <summary>The dataset file's shape: <c>{ "tickets": [ ... ] }</c>.</summary>
public sealed record SupportTicketFile(IReadOnlyList<SupportTicket> Tickets);
