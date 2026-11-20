using System.Net;
using FabricPilot.Modules.Knowledge.Application;
using FabricPilot.Modules.Knowledge.Application.Commands.CreateSample;
using FabricPilot.Modules.Knowledge.Application.Queries.GetSample;
using FabricPilot.Modules.Knowledge.Domain.Samples;

namespace FabricPilot.Modules.Knowledge.Tests.Samples;

public class GetSampleTests
{
    [Fact]
    public async Task A_created_sample_is_read_back_by_its_id()
    {
        var store = new InMemorySampleStore();
        var created = await new CreateSampleHandler(store, store).HandleAsync(new CreateSample(SampleName.From("Ada")), TestContext.Current.CancellationToken);

        var sample = await new GetSampleHandler(store).HandleAsync(new GetSample(created.Value), TestContext.Current.CancellationToken);

        sample.Value.Id.ShouldBe(created.Value);
        sample.Value.Name.ShouldBe("Ada");
    }

    [Fact]
    public async Task An_unknown_id_and_the_empty_guid_are_not_found()
    {
        var handler = new GetSampleHandler(new InMemorySampleStore());

        var unknown = await handler.HandleAsync(new GetSample(Guid.CreateVersion7()), TestContext.Current.CancellationToken);
        var empty = await handler.HandleAsync(new GetSample(Guid.Empty), TestContext.Current.CancellationToken);

        unknown.Error.Code.ShouldBe("knowledge.sample-not-found");
        unknown.Error.Status.ShouldBe(HttpStatusCode.NotFound);
        empty.Error.Code.ShouldBe("knowledge.sample-not-found");
    }

    [Fact]
    public void A_name_the_caller_got_wrong_is_a_bad_request_with_its_own_code()
    {
        var error = SampleFailures.InvalidName();

        error.Code.ShouldBe("knowledge.invalid-sample-name");
        error.Status.ShouldBe(HttpStatusCode.BadRequest);
    }
}
