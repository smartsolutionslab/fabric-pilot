using FabricPilot.Modules.Contracts.Domain.Samples;

namespace FabricPilot.Modules.Contracts.Application.DTOs;

public static class SampleMappings
{
    public static ContractsSampleDto ToDto(this Sample sample)
    {
        Ensure.That(sample).IsNotNull();

        return new ContractsSampleDto(sample.Id.Value, sample.Name.Value);
    }
}
