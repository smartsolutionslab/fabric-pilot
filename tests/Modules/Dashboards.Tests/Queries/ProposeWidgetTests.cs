using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Dashboards.Application.DTOs;
using FabricPilot.Modules.Dashboards.Application.Queries.ProposeWidget;
using FabricPilot.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace FabricPilot.Modules.Dashboards.Tests.Queries;

/// <summary>The dashboards agent's proposal, through the dispatcher: a declared widget as a fitting visual, never saved.</summary>
public class ProposeWidgetTests
{
    [Fact]
    public async Task A_declared_chart_is_proposed_with_its_declared_title()
    {
        var result = await ProposeAsync(new ProposeWidget("dashboards.widget-counts", "line", Option<string>.None));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Source.ShouldBe("dashboards.widget-counts");
        result.Value.Title.ShouldBe("Widgets per dashboard");
        result.Value.Visual.ShouldBe("line");
        result.Value.Replaces.ShouldBeNull();
    }

    [Fact]
    public async Task A_proposal_names_the_draft_widget_it_replaces()
    {
        var result = await ProposeAsync(new ProposeWidget("dashboards.my-dashboards", "table", Option<string>.Some("w-1")));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Replaces.ShouldBe("w-1");
    }

    [Fact]
    public async Task A_widget_the_modules_do_not_declare_is_refused_with_its_code()
    {
        var result = await ProposeAsync(new ProposeWidget("billing.everything", "bar", Option<string>.None));

        result.IsSuccess.ShouldBeFalse();
        result.Error.Code.ShouldBe("dashboards.unknown-widget");
    }

    [Theory]
    [InlineData("dashboards.my-dashboards", "bar")]
    [InlineData("dashboards.widget-counts", "table")]
    [InlineData("dashboards.widget-counts", "radar")]
    public async Task A_visual_that_does_not_fit_the_widget_is_refused_with_its_code(string source, string visual)
    {
        var result = await ProposeAsync(new ProposeWidget(source, visual, Option<string>.None));

        result.IsSuccess.ShouldBeFalse();
        result.Error.Code.ShouldBe("dashboards.invalid-widget");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("an-identifier-of-sixty-five-characters-is-one-more-than-a-key-has")]
    public async Task A_replaced_key_must_be_one_a_dashboard_can_hold(string key)
    {
        var result = await ProposeAsync(new ProposeWidget("dashboards.widget-counts", "bar", Option<string>.Some(key)));

        result.IsSuccess.ShouldBeFalse();
        result.Error.Code.ShouldBe("dashboards.invalid-widget");
    }

    private static async Task<Result<WidgetProposalDto, ApiError>> ProposeAsync(ProposeWidget query)
    {
        using var model = new FakeChatClient("unused");
        await using var services = DashboardServices.Build(model);
        await using var scope = services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IDispatcher>().QueryAsync(query, TestContext.Current.CancellationToken);
    }
}
