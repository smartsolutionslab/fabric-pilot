namespace FabricPilot.Contracts;

/// <summary>
/// What every integration event carries besides its own data, filled by the unit of work that publishes it. A
/// redelivery carries the same metadata, so a consumer deduplicates on <see cref="EventIdentifier"/>.
/// </summary>
/// <param name="EventIdentifier">Guid v7, one per published event; the same on every delivery of it.</param>
/// <param name="OccurredAt">When the event was published.</param>
/// <param name="TenantId">
/// The tenant whose work published the event; null for an event that a write of records shared by all tenants published
/// (an endpoint mapped with <c>WritesSharedRecords()</c>), which concerns every tenant and belongs to none.
/// </param>
public sealed record EventMetadata(Guid EventIdentifier, DateTimeOffset OccurredAt, string? TenantId);
