using FabricPilot.Modules.Tickets.Domain.Samples;

namespace FabricPilot.Modules.Tickets.Application.DTOs;

public static class SampleMappings
{
    public static TicketsSampleDto ToDto(this Sample sample)
    {
        Ensure.That(sample).IsNotNull();

        return new TicketsSampleDto(sample.Id.Value, sample.Name.Value);
    }
}
