using FabricPilot.Modules.Contracts.Application.Queries.AskTheModel;
using FabricPilot.Tests.Fakes;

namespace FabricPilot.Modules.Contracts.Tests.Queries;

public class AskTheModelTests
{
    [Fact]
    public async Task The_answer_is_the_text_the_model_gave_to_the_question_it_was_asked()
    {
        using var model = new FakeChatClient("Paid on 3 March.");
        var handler = new AskTheModelHandler(model);

        var result = await handler.HandleAsync(new AskTheModel("When was invoice 7 paid?"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("Paid on 3 March.");
        model.Requests.ShouldHaveSingleItem().ShouldHaveSingleItem().Text.ShouldBe("When was invoice 7 paid?");
    }
}
