using FabricPilot.BuildingBlocks.Domain;
using FabricPilot.Modules.Notifications.Domain.Samples.Events;

namespace FabricPilot.Modules.Notifications.Domain.Samples;

/// <summary>
/// The module's sample aggregate, emitted by fabric add-module: a record with a name that the sample's endpoints create,
/// list and read back. Rename it after the module's first aggregate (<c>Invoice</c>) or delete it with everything named
/// Sample (AGENTS.md).
/// </summary>
public sealed class Sample : AggregateRoot<SampleId>
{
    private Sample(SampleId id, SampleName name)
        : base(id) => Name = name;

    public SampleName Name { get; }

    /// <summary>A new sample, raising <see cref="SampleCreated"/>.</summary>
    public static Sample Create(SampleId id, SampleName name)
    {
        Ensure.That(id).IsNotNull();
        Ensure.That(name).IsNotNull();

        var sample = new Sample(id, name);
        sample.Raise(new SampleCreated(id, name));
        return sample;
    }
}
