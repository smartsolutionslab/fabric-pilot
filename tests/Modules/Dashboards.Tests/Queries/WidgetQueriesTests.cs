using FabricPilot.BuildingBlocks.Dashboards;
using FabricPilot.Modules.Dashboards.Application.Queries.DescribeWidget;
using FabricPilot.Modules.Dashboards.Application.Queries.ListWidgets;
using FabricPilot.Modules.Dashboards.Application.Queries.RunWidget;

namespace FabricPilot.Modules.Dashboards.Tests.Queries;

/// <summary>The declared widgets through the dispatcher: listed, described and run for a caller who may see them, and for no one else.</summary>
public class WidgetQueriesTests
{
    [Fact]
    public async Task The_widgets_the_caller_may_see_are_listed_a_page_at_a_time()
    {
        await using var module = new DashboardsModuleUnderTest();
        PageSize.TryFrom(1, out var one);

        var result = await module.QueryAsync(new ListWidgets(Page.First, one!), TestContext.Current.CancellationToken);

        result.Value.TotalCount.ShouldBe(2);
        result.Value.Items.Select(widget => $"{widget.Id} {widget.Title} {widget.Kind}").ShouldBe(["dashboards.my-dashboards My dashboards Table"]);
    }

    [Fact]
    public async Task A_widget_the_caller_may_not_see_is_neither_listed_described_nor_run()
    {
        await using var module = new DashboardsModuleUnderTest(DashboardsModuleUnderTest.Owner, "dashboards.my-dashboards");

        var listed = await module.QueryAsync(new ListWidgets(Page.First, PageSize.Default), TestContext.Current.CancellationToken);
        var described = await module.QueryAsync(new DescribeWidget("dashboards.my-dashboards"), TestContext.Current.CancellationToken);
        var run = await module.QueryAsync(new RunWidget("dashboards.my-dashboards", Page.First, PageSize.Default), TestContext.Current.CancellationToken);

        listed.Value.Items.Select(widget => widget.Id).ShouldBe(["dashboards.widget-counts"]);
        described.Error.Code.ShouldBe("dashboards.widget-not-found");
        run.Error.Code.ShouldBe("dashboards.widget-not-found");
    }

    [Fact]
    public async Task A_declared_widget_is_described_by_its_title_and_kind()
    {
        await using var module = new DashboardsModuleUnderTest();

        var result = await module.QueryAsync(new DescribeWidget("dashboards.widget-counts"), TestContext.Current.CancellationToken);

        result.Value.Title.ShouldBe("Widgets per dashboard");
        result.Value.Kind.ShouldBe(WidgetKind.Chart);
    }

    [Fact]
    public async Task A_widget_the_modules_do_not_declare_is_neither_described_nor_run()
    {
        await using var module = new DashboardsModuleUnderTest();

        var described = await module.QueryAsync(new DescribeWidget("billing.everything"), TestContext.Current.CancellationToken);
        var run = await module.QueryAsync(new RunWidget("billing.everything", Page.First, PageSize.Default), TestContext.Current.CancellationToken);

        described.Error.Code.ShouldBe("dashboards.widget-not-found");
        run.Error.Code.ShouldBe("dashboards.widget-not-found");
    }

    [Fact]
    public async Task A_chart_widget_runs_its_query_as_the_caller()
    {
        await using var module = new DashboardsModuleUnderTest();
        await SavedDashboards.SaveAsync(module, "Sales", 2, TestContext.Current.CancellationToken);

        var result = await module.QueryAsync(new RunWidget("dashboards.widget-counts", Page.First, PageSize.Default), TestContext.Current.CancellationToken);

        result.Value.Kind.ShouldBe(WidgetKind.Chart);
        result.Value.Chart!.Categories.ShouldBe(["Sales"]);
    }

    [Fact]
    public async Task A_table_widget_runs_its_query_for_the_page_it_is_given()
    {
        await using var module = new DashboardsModuleUnderTest();
        await SavedDashboards.SaveAsync(module, "Sales", 2, TestContext.Current.CancellationToken);
        await SavedDashboards.SaveAsync(module, "Costs", 1, TestContext.Current.CancellationToken);
        PageSize.TryFrom(1, out var one);

        var result = await module.QueryAsync(new RunWidget("dashboards.my-dashboards", Page.First, one!), TestContext.Current.CancellationToken);

        result.Value.Kind.ShouldBe(WidgetKind.Table);
        result.Value.Table!.TotalCount.ShouldBe(2);
        result.Value.Table!.Rows.Single().GetProperty("name").GetString().ShouldBe("Costs");
    }
}
