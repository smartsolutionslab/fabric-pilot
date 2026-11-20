using FabricPilot.Modules.Billing.Domain.Samples;

namespace FabricPilot.Modules.Billing.Application.DTOs;

public static class SampleMappings
{
    public static BillingSampleDto ToDto(this Sample sample)
    {
        Ensure.That(sample).IsNotNull();

        return new BillingSampleDto(sample.Id.Value, sample.Name.Value);
    }
}
