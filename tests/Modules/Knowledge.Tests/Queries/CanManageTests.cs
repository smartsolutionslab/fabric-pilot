using FabricPilot.Modules.Knowledge.Application;
using FabricPilot.Modules.Knowledge.Application.Queries.CanManage;

namespace FabricPilot.Modules.Knowledge.Tests.Queries;

public class CanManageTests
{
    [Fact]
    public async Task The_answer_names_the_permission_the_endpoints_policy_granted()
    {
        var handler = new CanManageHandler();

        var result = await handler.HandleAsync(new CanManage(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe(KnowledgePolicyNames.Manage);
    }
}
