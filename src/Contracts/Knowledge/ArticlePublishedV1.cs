namespace FabricPilot.Contracts.Knowledge;

/// <summary>
/// Staff published an article of the knowledge base every tenant shares; it is searchable from now on. Published inside
/// <c>WritesSharedRecords()</c>, so its <see cref="EventMetadata.TenantId"/> is null: it concerns every tenant.
/// </summary>
public sealed record ArticlePublishedV1(Guid ArticleId, string Title) : IntegrationEvent;
