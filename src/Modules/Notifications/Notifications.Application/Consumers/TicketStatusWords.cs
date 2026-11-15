namespace FabricPilot.Modules.Notifications.Application.Consumers;

/// <summary>
/// The words a customer reads for each ticket status, which <c>TicketStatusChangedV1</c> carries by name. A name not
/// listed here is a status the notifications module was not taught: telling the customer its name would show them an
/// identifier, so it fails instead and the event is retried and dead-lettered.
/// </summary>
public static class TicketStatusWords
{
    private static readonly Dictionary<string, string> Words = new(StringComparer.Ordinal)
    {
        ["Open"] = "open",
        ["InProgress"] = "in progress",
        ["Resolved"] = "resolved",
        ["Closed"] = "closed",
    };

    public static string For(string status) =>
        Words.TryGetValue(status, out var words)
            ? words
            : throw new InvalidOperationException($"The notifications module has no words for the ticket status \"{status}\".");
}
