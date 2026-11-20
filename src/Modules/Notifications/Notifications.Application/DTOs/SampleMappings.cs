using FabricPilot.Modules.Notifications.Domain.Samples;

namespace FabricPilot.Modules.Notifications.Application.DTOs;

public static class SampleMappings
{
    public static NotificationsSampleDto ToDto(this Sample sample)
    {
        Ensure.That(sample).IsNotNull();

        return new NotificationsSampleDto(sample.Id.Value, sample.Name.Value);
    }
}
