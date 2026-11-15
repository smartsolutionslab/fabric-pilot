namespace FabricPilot.Modules.Notifications.Application;

/// <summary>A plain-text e-mail to one address; the sender is the module's configured address.</summary>
public sealed record EmailMessage(string To, string Subject, string Body);
