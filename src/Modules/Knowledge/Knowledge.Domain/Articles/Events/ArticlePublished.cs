using FabricPilot.BuildingBlocks.Domain;

namespace FabricPilot.Modules.Knowledge.Domain.Articles.Events;

[DeliberatelyUnhandled("Publishing indexes the article and announces ArticlePublishedV1 in the staff request itself: the shared index is written only inside WritesSharedRecords(), which a reaction's own scope is not.")]
public sealed record ArticlePublished(ArticleId Article) : IDomainEvent;
