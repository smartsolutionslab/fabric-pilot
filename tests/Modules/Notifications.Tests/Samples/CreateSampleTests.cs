using FabricPilot.Modules.Notifications.Application.Commands.CreateSample;
using FabricPilot.Modules.Notifications.Domain.Samples;

namespace FabricPilot.Modules.Notifications.Tests.Samples;

public class CreateSampleTests
{
    [Fact]
    public async Task A_created_sample_is_stored_with_its_name_once_the_unit_of_work_commits_and_its_id_answered()
    {
        var store = new InMemorySampleStore();

        var created = await new CreateSampleHandler(store, store).HandleAsync(new CreateSample(SampleName.From("Ada")), TestContext.Current.CancellationToken);

        var sample = store.Stored.ShouldHaveSingleItem();
        created.Value.ShouldBe(sample.Id.Value);
        sample.Name.Value.ShouldBe("Ada");
        created.Value.Version.ShouldBe(7);
    }
}
