using System.Security.Claims;
using FabricPilot.BuildingBlocks.Api;
using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Identity.Application;
using FabricPilot.Modules.Identity.Application.Commands.ChangeTenantUserRole;
using FabricPilot.Modules.Identity.Application.Commands.InviteTenantUser;
using FabricPilot.Modules.Identity.Application.Commands.ResendInvitation;
using FabricPilot.Modules.Identity.Application.Queries.ListTenantUsers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FabricPilot.Modules.Identity.Api.Endpoints;

/// <summary>A customer admin manages the users of their own tenant, the one their token names.</summary>
public sealed class TenantUserEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        Ensure.That(endpoints).IsNotNull();

        var users = endpoints.MapGroup("/users")
            .WithTags("Identity")
            .RequireAuthorization(IdentityPolicyNames.ManageUsers);

        users.MapGet(string.Empty, async (Page? page, PageSize? pageSize, IDispatcher dispatcher, CancellationToken cancellationToken) =>
            (await dispatcher.QueryAsync(new ListTenantUsers(page ?? Page.First, pageSize ?? PageSize.Default), cancellationToken)).ToHttpResult());

        users.MapPost(string.Empty, async (InvitationRequest request, ClaimsPrincipal user, IDispatcher dispatcher, CancellationToken cancellationToken) =>
        {
            var (email, firstName, lastName, role) = request;
            var inviteTenantUser = new InviteTenantUser(email, firstName, lastName, role, Caller.Of(user));
            return (await dispatcher.SendAsync(inviteTenantUser, cancellationToken))
                .ToHttpResult(id => TypedResults.Created($"/identity/users/{id}", id));
        });

        users.MapPut("/{userId:guid}/role", async (Guid userId, RoleRequest request, ClaimsPrincipal user, IDispatcher dispatcher, CancellationToken cancellationToken) =>
            (await dispatcher.SendAsync(new ChangeTenantUserRole(userId, request.Role, Caller.Of(user)), cancellationToken))
                .ToHttpResult(_ => TypedResults.NoContent()));

        users.MapPut("/{userId:guid}/invitation", async (Guid userId, ClaimsPrincipal user, IDispatcher dispatcher, CancellationToken cancellationToken) =>
            (await dispatcher.SendAsync(new ResendInvitation(userId, Caller.Of(user)), cancellationToken))
                .ToHttpResult(_ => TypedResults.NoContent()));
    }
}
