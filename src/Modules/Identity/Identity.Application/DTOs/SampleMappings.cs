using FabricPilot.Modules.Identity.Domain.Samples;

namespace FabricPilot.Modules.Identity.Application.DTOs;

public static class SampleMappings
{
    public static IdentitySampleDto ToDto(this Sample sample)
    {
        Ensure.That(sample).IsNotNull();

        return new IdentitySampleDto(sample.Id.Value, sample.Name.Value);
    }
}
