using FabricPilot.Modules.Documents.Domain.Samples;
using FabricPilot.Modules.Documents.Domain.Samples.Events;

namespace FabricPilot.Modules.Documents.Tests.Samples;

public class SampleTests
{
    [Fact]
    public void A_new_sample_has_its_id_and_name_and_raises_that_it_was_created()
    {
        var id = SampleId.New();

        var sample = Sample.Create(id, SampleName.From("Ada"));

        sample.Id.ShouldBe(id);
        sample.Name.ShouldBe(SampleName.From("Ada"));
        sample.DomainEvents.ShouldHaveSingleItem().ShouldBe(new SampleCreated(id, SampleName.From("Ada")));
    }
}
