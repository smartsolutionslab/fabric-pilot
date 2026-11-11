using FabricPilot.Modules.Dashboards.Application.Commands.RemoveDashboard;

namespace FabricPilot.Modules.Dashboards.Tests.Commands;

/// <summary>Removing a dashboard through the dispatcher: the caller's own, and nobody else's.</summary>
public class RemoveDashboardTests
{
    [Fact]
    public async Task The_callers_dashboard_is_removed()
    {
        await using var module = new DashboardsModuleUnderTest();
        var id = await SavedDashboards.SaveAsync(module, "Sales", 1, TestContext.Current.CancellationToken);

        var result = await module.SendAsync(new RemoveDashboard(id), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        module.Store.Stored.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_dashboard_the_caller_does_not_have_is_not_found()
    {
        await using var module = new DashboardsModuleUnderTest();

        var result = await module.SendAsync(new RemoveDashboard(Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe("dashboards.dashboard-not-found");
    }

    [Fact]
    public async Task A_dashboard_of_another_user_in_the_same_table_is_not_found_and_stays()
    {
        await using var module = new DashboardsModuleUnderTest();
        await using var other = module.SignedInAs(DashboardsModuleUnderTest.OtherOwner);
        var id = await SavedDashboards.SaveAsync(other, "Sales", 1, TestContext.Current.CancellationToken);

        var result = await module.SendAsync(new RemoveDashboard(id), TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe("dashboards.dashboard-not-found");
        module.Store.Stored.Single().Owner.ShouldBe(DashboardsModuleUnderTest.OtherOwner);
    }

    [Fact]
    public async Task Without_a_signed_in_user_nothing_is_removed()
    {
        await using var module = new DashboardsModuleUnderTest(null);

        var result = await module.SendAsync(new RemoveDashboard(Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe("dashboards.not-signed-in");
        module.Store.Saves.ShouldBe(0);
    }
}
