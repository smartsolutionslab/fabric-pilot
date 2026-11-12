namespace FabricPilot.Modules.Contracts.Domain.Contracts;

/// <summary>What happens at the end of a contract's last day.</summary>
public enum ContractRenewal
{
    /// <summary>A new term starts the next day.</summary>
    Renews,

    /// <summary>The contract ends; nothing follows it.</summary>
    Ends,
}
