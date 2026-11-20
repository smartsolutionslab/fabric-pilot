using FabricPilot.Modules.Knowledge.Domain.Samples;

namespace FabricPilot.Modules.Knowledge.Application.DTOs;

public static class SampleMappings
{
    public static KnowledgeSampleDto ToDto(this Sample sample)
    {
        Ensure.That(sample).IsNotNull();

        return new KnowledgeSampleDto(sample.Id.Value, sample.Name.Value);
    }
}
