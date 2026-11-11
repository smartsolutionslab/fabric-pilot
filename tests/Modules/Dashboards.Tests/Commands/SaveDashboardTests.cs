using FabricPilot.Modules.Dashboards.Application.Commands.SaveDashboard;
using FabricPilot.Modules.Dashboards.Application.Queries.GetDashboard;

namespace FabricPilot.Modules.Dashboards.Tests.Commands;

/// <summary>Saving the caller's dashboard through the dispatcher: what it keeps, and everything it refuses unsaved.</summary>
public class SaveDashboardTests
{
    private static readonly Guid Id = Guid.CreateVersion7();

    [Fact]
    public async Task A_saved_dashboard_reads_back_with_its_name_and_each_widget_where_it_was_placed()
    {
        await using var module = new DashboardsModuleUnderTest();

        var saved = await module.SendAsync(Save(" Sales ", new WidgetPlacement("w-1", "dashboards.widget-counts", "pie", 2, 3, 4, 5), new WidgetPlacement("w-2", "dashboards.my-dashboards", "table", 0, 8, 12, 6)), TestContext.Current.CancellationToken);
        var read = await module.QueryAsync(new GetDashboard(Id), TestContext.Current.CancellationToken);

        saved.Value.ShouldBe(Id);
        read.Value.Id.ShouldBe(Id);
        read.Value.Name.ShouldBe("Sales");
        read.Value.Widgets.Select(widget => $"{widget.Key} {widget.Source} {widget.Visual} {widget.X},{widget.Y} {widget.Width}x{widget.Height}").ShouldBe(["w-1 dashboards.widget-counts pie 2,3 4x5", "w-2 dashboards.my-dashboards table 0,8 12x6"]);
        module.Store.Stored.Single().Owner.ShouldBe(DashboardsModuleUnderTest.Owner);
    }

    [Fact]
    public async Task Saving_the_same_id_again_replaces_the_name_and_the_widgets_of_the_one_dashboard()
    {
        await using var module = new DashboardsModuleUnderTest();
        await module.SendAsync(Save("Sales", new WidgetPlacement("w-1", "dashboards.widget-counts", "bar", 0, 0, 6, 4)), TestContext.Current.CancellationToken);

        await module.SendAsync(Save("Costs", new WidgetPlacement("w-2", "dashboards.widget-counts", "line", 6, 0, 6, 4)), TestContext.Current.CancellationToken);

        var dashboard = module.Store.Stored.Single();
        dashboard.Name.ShouldBe("Costs");
        dashboard.Widgets.Select(widget => widget.Key).ShouldBe(["w-2"]);
    }

    [Fact]
    public async Task Without_a_signed_in_user_nothing_is_saved()
    {
        await using var module = new DashboardsModuleUnderTest(null);

        var result = await module.SendAsync(Save("Sales"), TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe("dashboards.not-signed-in");
        module.Store.Saves.ShouldBe(0);
    }

    [Fact]
    public async Task The_empty_id_is_refused()
    {
        await using var module = new DashboardsModuleUnderTest();

        var result = await module.SendAsync(new SaveDashboard(Guid.Empty, "Sales", []), TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe("dashboards.invalid-id");
        module.Store.Saves.ShouldBe(0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("A name of more than one hundred characters is longer than a dashboard's name may be, so saving it is refused.")]
    public async Task A_name_that_is_blank_or_too_long_is_refused(string name)
    {
        await using var module = new DashboardsModuleUnderTest();

        var result = await module.SendAsync(Save(name), TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe("dashboards.invalid-name");
        module.Store.Saves.ShouldBe(0);
    }

    [Fact]
    public async Task More_widgets_than_a_dashboard_holds_are_refused()
    {
        await using var module = new DashboardsModuleUnderTest();
        WidgetPlacement[] widgets = [.. Enumerable.Range(0, SaveDashboardHandler.MaximumWidgets + 1).Select(index => new WidgetPlacement($"w-{index}", "dashboards.widget-counts", "bar", 0, index, 1, 1))];

        var result = await module.SendAsync(Save("Sales", widgets), TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe("dashboards.too-many-widgets");
    }

    [Theory]
    [InlineData("")]
    [InlineData("an-identifier-of-sixty-five-characters-is-one-more-than-a-key-has")]
    public async Task A_widget_key_that_is_blank_or_too_long_is_refused(string key)
    {
        await using var module = new DashboardsModuleUnderTest();

        var result = await module.SendAsync(Save("Sales", new WidgetPlacement(key, "dashboards.widget-counts", "bar", 0, 0, 1, 1)), TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe("dashboards.invalid-widget");
    }

    [Fact]
    public async Task Two_widgets_with_one_key_are_refused()
    {
        await using var module = new DashboardsModuleUnderTest();

        var result = await module.SendAsync(Save("Sales", new WidgetPlacement("w-1", "dashboards.widget-counts", "bar", 0, 0, 1, 1), new WidgetPlacement("w-1", "dashboards.widget-counts", "pie", 1, 0, 1, 1)), TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe("dashboards.invalid-widget");
    }

    [Fact]
    public async Task A_widget_the_modules_do_not_declare_is_refused()
    {
        await using var module = new DashboardsModuleUnderTest();

        var result = await module.SendAsync(Save("Sales", new WidgetPlacement("w-1", "billing.everything", "bar", 0, 0, 1, 1)), TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe("dashboards.unknown-widget");
    }

    [Fact]
    public async Task A_declared_widget_the_caller_may_not_see_is_refused_as_unknown()
    {
        await using var module = new DashboardsModuleUnderTest(DashboardsModuleUnderTest.Owner, "dashboards.widget-counts");

        var result = await module.SendAsync(Save("Sales", new WidgetPlacement("w-1", "dashboards.widget-counts", "bar", 0, 0, 1, 1)), TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe("dashboards.unknown-widget");
    }

    [Theory]
    [InlineData("dashboards.widget-counts", "table")]
    [InlineData("dashboards.my-dashboards", "bar")]
    public async Task A_visual_the_widget_cannot_show_is_refused(string source, string visual)
    {
        await using var module = new DashboardsModuleUnderTest();

        var result = await module.SendAsync(Save("Sales", new WidgetPlacement("w-1", source, visual, 0, 0, 1, 1)), TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe("dashboards.invalid-widget");
    }

    [Theory]
    [InlineData(-1, 0, 1, 1)]
    [InlineData(0, 0, 0, 1)]
    [InlineData(0, 0, 13, 1)]
    [InlineData(12, 0, 1, 1)]
    [InlineData(0, -1, 1, 1)]
    [InlineData(0, 1001, 1, 1)]
    [InlineData(0, 0, 1, 0)]
    [InlineData(0, 0, 1, 25)]
    public async Task A_widget_outside_the_grid_is_refused(int x, int y, int width, int height)
    {
        await using var module = new DashboardsModuleUnderTest();

        var result = await module.SendAsync(Save("Sales", new WidgetPlacement("w-1", "dashboards.widget-counts", "bar", x, y, width, height)), TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe("dashboards.invalid-widget");
        module.Store.Saves.ShouldBe(0);
    }

    private static SaveDashboard Save(string name, params WidgetPlacement[] widgets) => new(Id, name, widgets);
}
