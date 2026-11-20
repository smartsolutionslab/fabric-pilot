using FabricPilot.Modules.Billing.Application.Commands.CreateSample;
using FabricPilot.Modules.Billing.Application.Queries.ListSamples;
using FabricPilot.Modules.Billing.Domain.Samples;

namespace FabricPilot.Modules.Billing.Tests.Samples;

public class ListSamplesTests
{
    [Fact]
    public async Task Samples_are_listed_by_name_a_page_at_a_time_with_how_many_there_are()
    {
        var store = new InMemorySampleStore();
        foreach (var name in new[] { "Cleo", "Ada", "Bea" })
        {
            await new CreateSampleHandler(store, store).HandleAsync(new CreateSample(SampleName.From(name)), TestContext.Current.CancellationToken);
        }

        var page = await new ListSamplesHandler(store).HandleAsync(new ListSamples(PageOf(2), PageSizeOf(2)), TestContext.Current.CancellationToken);

        page.Value.Items.ShouldHaveSingleItem().Name.ShouldBe("Cleo");
        page.Value.TotalCount.ShouldBe(3);
        page.Value.Page.ShouldBe(2);
        page.Value.PageSize.ShouldBe(2);
    }

    [Fact]
    public async Task Without_samples_the_first_page_is_empty()
    {
        var page = await new ListSamplesHandler(new InMemorySampleStore()).HandleAsync(new ListSamples(Page.First, PageSize.Default), TestContext.Current.CancellationToken);

        page.Value.Items.ShouldBeEmpty();
        page.Value.TotalCount.ShouldBe(0);
    }

    private static Page PageOf(int value) =>
        Page.TryFrom(value, out var page) ? page : throw new InvalidOperationException($"{value} is not a page.");

    private static PageSize PageSizeOf(int value) =>
        PageSize.TryFrom(value, out var pageSize) ? pageSize : throw new InvalidOperationException($"{value} is not a page size.");
}
