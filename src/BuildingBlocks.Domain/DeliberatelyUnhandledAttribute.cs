namespace FabricPilot.BuildingBlocks.Domain;

/// <summary>
/// Marks a domain event no reaction is meant to react to, with the reason. Without it, the architecture rule
/// EveryDomainEventHasAReaction fails an event that has no reaction.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class DeliberatelyUnhandledAttribute : Attribute
{
    public DeliberatelyUnhandledAttribute(string reason)
    {
        Ensure.That(reason).IsNotNullOrWhiteSpace();
        Reason = reason;
    }

    public string Reason { get; }
}
