using FabricPilot.Modules.Documents.Domain.Samples;

namespace FabricPilot.Modules.Documents.Application.DTOs;

public static class SampleMappings
{
    public static DocumentsSampleDto ToDto(this Sample sample)
    {
        Ensure.That(sample).IsNotNull();

        return new DocumentsSampleDto(sample.Id.Value, sample.Name.Value);
    }
}
