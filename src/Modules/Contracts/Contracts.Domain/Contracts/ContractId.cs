namespace FabricPilot.Modules.Contracts.Domain.Contracts;

public sealed record ContractId(Guid Value)
{
    public static ContractId New() => new(Guid.CreateVersion7());
}
