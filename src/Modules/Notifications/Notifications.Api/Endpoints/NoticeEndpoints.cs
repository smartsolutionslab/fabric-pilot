using System.Security.Claims;
using FabricPilot.BuildingBlocks.Api;
using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Notifications.Application;
using FabricPilot.Modules.Notifications.Application.Queries.ListNotices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FabricPilot.Modules.Notifications.Api.Endpoints;

/// <summary>The signed-in customer's own notices, in their tenant.</summary>
public sealed class NoticeEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        Ensure.That(endpoints).IsNotNull();

        endpoints.MapGroup("/notices").WithTags("Notifications")
            .MapGet(string.Empty, async (Page? page, PageSize? pageSize, ClaimsPrincipal user, IDispatcher dispatcher, CancellationToken cancellationToken) =>
                (await dispatcher.QueryAsync(new ListNotices(Recipient(user), page ?? Page.First, pageSize ?? PageSize.Default), cancellationToken)).ToHttpResult())
            .RequireAuthorization(NotificationsPolicyNames.ReadOwnNotices);
    }

    /// <summary>The caller's Keycloak user id; a token without one is a broken identity provider, not a caller's mistake.</summary>
    private static string Recipient(ClaimsPrincipal user) =>
        (user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub"))?.Value
            ?? throw new InvalidOperationException("The caller's token carries no user id (sub).");
}
