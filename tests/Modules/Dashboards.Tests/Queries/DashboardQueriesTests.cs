using FabricPilot.Modules.Dashboards.Application.Queries.CountWidgets;
using FabricPilot.Modules.Dashboards.Application.Queries.GetDashboard;
using FabricPilot.Modules.Dashboards.Application.Queries.ListDashboards;

namespace FabricPilot.Modules.Dashboards.Tests.Queries;

/// <summary>Reading the caller's dashboards through the dispatcher: one, a page of them, and the widgets each holds.</summary>
public class DashboardQueriesTests
{
    [Fact]
    public async Task A_dashboard_of_another_user_in_the_same_table_is_not_found()
    {
        await using var module = new DashboardsModuleUnderTest();
        await using var other = module.SignedInAs(DashboardsModuleUnderTest.OtherOwner);
        var id = await SavedDashboards.SaveAsync(other, "Sales", 1, TestContext.Current.CancellationToken);

        var result = await module.QueryAsync(new GetDashboard(id), TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe("dashboards.dashboard-not-found");
    }

    [Fact]
    public async Task The_callers_dashboards_are_listed_a_page_at_a_time_by_name_with_their_widget_counts()
    {
        await using var module = new DashboardsModuleUnderTest();
        await using var other = module.SignedInAs(DashboardsModuleUnderTest.OtherOwner);
        await SavedDashboards.SaveAsync(other, "Accounts", 4, TestContext.Current.CancellationToken);
        await SavedDashboards.SaveAsync(module, "Costs", 2, TestContext.Current.CancellationToken);
        await SavedDashboards.SaveAsync(module, "Audit", 0, TestContext.Current.CancellationToken);
        await SavedDashboards.SaveAsync(module, "Sales", 1, TestContext.Current.CancellationToken);
        PageSize.TryFrom(2, out var two);

        var result = await module.QueryAsync(new ListDashboards(Page.First, two!), TestContext.Current.CancellationToken);

        result.Value.TotalCount.ShouldBe(3);
        result.Value.Items.Select(dashboard => $"{dashboard.Name} {dashboard.WidgetCount}").ShouldBe(["Audit 0", "Costs 2"]);
    }

    [Fact]
    public async Task The_chart_counts_the_widgets_of_each_of_the_callers_dashboards_and_of_no_one_elses()
    {
        await using var module = new DashboardsModuleUnderTest();
        await using var other = module.SignedInAs(DashboardsModuleUnderTest.OtherOwner);
        await SavedDashboards.SaveAsync(module, "Sales", 3, TestContext.Current.CancellationToken);
        await SavedDashboards.SaveAsync(other, "Accounts", 4, TestContext.Current.CancellationToken);
        await SavedDashboards.SaveAsync(module, "Costs", 1, TestContext.Current.CancellationToken);

        var result = await module.QueryAsync(new CountWidgets(), TestContext.Current.CancellationToken);

        result.Value.Categories.ShouldBe(["Costs", "Sales"]);
        result.Value.Series.Single().Values.ShouldBe([1m, 3m]);
    }

    [Fact]
    public async Task Without_a_signed_in_user_nothing_is_read()
    {
        await using var module = new DashboardsModuleUnderTest(null);

        var dashboard = await module.QueryAsync(new GetDashboard(Guid.CreateVersion7()), TestContext.Current.CancellationToken);
        var page = await module.QueryAsync(new ListDashboards(Page.First, PageSize.Default), TestContext.Current.CancellationToken);
        var chart = await module.QueryAsync(new CountWidgets(), TestContext.Current.CancellationToken);

        dashboard.Error.Code.ShouldBe("dashboards.not-signed-in");
        page.Error.Code.ShouldBe("dashboards.not-signed-in");
        chart.Error.Code.ShouldBe("dashboards.not-signed-in");
    }
}
