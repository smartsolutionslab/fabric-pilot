using FabricPilot.BuildingBlocks.Domain;

namespace FabricPilot.Modules.Tickets.Domain.Samples.Events;

[DeliberatelyUnhandled("The module's sample: nothing reacts to a sample being created. A real event gets a reaction (AGENTS.md).")]
public sealed record SampleCreated(SampleId Id, SampleName Name) : IDomainEvent;
