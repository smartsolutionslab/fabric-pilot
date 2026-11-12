using FabricPilot.Contracts;
using FabricPilot.BuildingBlocks.Tenancy;
using Microsoft.Extensions.DependencyInjection;

namespace FabricPilot.BuildingBlocks.Messaging;

/// <summary>How a unit of work fills the metadata of an event it publishes.</summary>
public static class IntegrationEventMetadata
{
    /// <summary>
    /// The event with new metadata: a Guid v7 identifier and the current time, and the current tenant. Called once per
    /// publish, before the event is saved, so every delivery of it carries the same metadata.
    /// </summary>
    public static IntegrationEvent Fill(IntegrationEvent integrationEvent, IServiceProvider services)
    {
        Ensure.That(integrationEvent).IsNotNull();
        Ensure.That(services).IsNotNull();

        var time = services.GetRequiredService<TimeProvider>();
        // Like a row, an event belongs to a tenant: one published without a tenant is refused, as saving one would be.
        var tenantId = services.GetService<ITenantContext>()?.TenantId
            ?? throw new InvalidOperationException($"{integrationEvent.GetType().Name} is published without a tenant; begin a TenantScope for work outside a request.");
        return integrationEvent with { Metadata = new EventMetadata(Guid.CreateVersion7(), time.GetUtcNow(), tenantId) };
    }
}
