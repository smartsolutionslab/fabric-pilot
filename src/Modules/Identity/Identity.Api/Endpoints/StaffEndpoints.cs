using FabricPilot.BuildingBlocks.Api;
using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Identity.Application;
using FabricPilot.Modules.Identity.Application.Queries.ListStaff;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FabricPilot.Modules.Identity.Api.Endpoints;

/// <summary>Staff list the realm's staff, whom a ticket can be assigned to. Read only, from Keycloak; no tenant's rows.</summary>
public sealed class StaffEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        Ensure.That(endpoints).IsNotNull();

        endpoints.MapGet("/staff", async (Page? page, PageSize? pageSize, IDispatcher dispatcher, CancellationToken cancellationToken) =>
                (await dispatcher.QueryAsync(new ListStaff(page ?? Page.First, pageSize ?? PageSize.Default), cancellationToken)).ToHttpResult())
            .WithTags("Identity")
            .RequireAuthorization(IdentityPolicyNames.ListStaff);
    }
}
