using FabricPilot.Modules.Documents.Application;
using FabricPilot.Modules.Documents.Application.Queries.CanManage;

namespace FabricPilot.Modules.Documents.Tests.Queries;

public class CanManageTests
{
    [Fact]
    public async Task The_answer_names_the_permission_the_endpoints_policy_granted()
    {
        var handler = new CanManageHandler();

        var result = await handler.HandleAsync(new CanManage(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe(DocumentsPolicyNames.Manage);
    }
}
