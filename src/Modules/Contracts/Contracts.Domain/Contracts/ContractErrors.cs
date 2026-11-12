namespace FabricPilot.Modules.Contracts.Domain.Contracts;

/// <summary>Every rule a contract can break, each with its own code.</summary>
public static class ContractErrors
{
    public static ContractError InvalidProduct(int maximum) => new InvalidProduct(maximum);

    public static ContractError EndsBeforeItStarts(DateOnly startsOn, DateOnly endsOn) => new EndsBeforeItStarts(startsOn, endsOn);
}
