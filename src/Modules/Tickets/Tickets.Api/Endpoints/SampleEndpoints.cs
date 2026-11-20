using FabricPilot.BuildingBlocks.Api;
using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Tickets.Application;
using FabricPilot.Modules.Tickets.Application.Commands.CreateSample;
using FabricPilot.Modules.Tickets.Application.Queries.GetSample;
using FabricPilot.Modules.Tickets.Application.Queries.ListSamples;
using FabricPilot.Modules.Tickets.Domain.Samples;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FabricPilot.Modules.Tickets.Api.Endpoints;

/// <summary>
/// The module's sample endpoints: create a sample, list them a page at a time, read one back. Rename them with the
/// sample or delete them (AGENTS.md).
/// </summary>
public sealed class SampleEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        Ensure.That(endpoints).IsNotNull();

        endpoints.MapPost("/samples", async (CreateTicketsSampleRequest request, IDispatcher dispatcher, CancellationToken cancellationToken) =>
        {
            // A name the caller got wrong is refused before it becomes a value object, whose factory would throw on it.
            if (!SampleName.TryFrom(request.Name, out var name))
            {
                return Result<Guid, ApiError>.Failure(SampleFailures.InvalidName()).ToCreatedHttpResult(Location);
            }

            return (await dispatcher.SendAsync(new CreateSample(name), cancellationToken)).ToCreatedHttpResult(Location);
        }).RequireAuthorization(TicketsPolicyNames.Manage);
        endpoints.MapGet("/samples", async (Page? page, PageSize? pageSize, IDispatcher dispatcher, CancellationToken cancellationToken) =>
            (await dispatcher.QueryAsync(new ListSamples(page ?? Page.First, pageSize ?? PageSize.Default), cancellationToken)).ToHttpResult());
        endpoints.MapGet("/samples/{id:guid}", async (Guid id, IDispatcher dispatcher, CancellationToken cancellationToken) =>
            (await dispatcher.QueryAsync(new GetSample(id), cancellationToken)).ToHttpResult());
    }

    private static string Location(Guid id) => $"/tickets/samples/{id}";
}
