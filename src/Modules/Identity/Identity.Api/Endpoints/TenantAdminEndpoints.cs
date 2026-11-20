using System.Security.Claims;
using FabricPilot.BuildingBlocks.Api;
using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Identity.Application;
using FabricPilot.Modules.Identity.Application.Commands.ChangeTenantAdminRole;
using FabricPilot.Modules.Identity.Application.Commands.InviteTenantAdmin;
using FabricPilot.Modules.Identity.Application.Queries.ListTenantAdmins;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FabricPilot.Modules.Identity.Api.Endpoints;

/// <summary>
/// Staff, who belong to no tenant, manage the admins of the tenant the route names. Not mapped with AcrossTenants(),
/// whose scope writes nothing: these endpoints write to Keycloak, for one named tenant, and touch no table (docs/adr).
/// </summary>
public sealed class TenantAdminEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        Ensure.That(endpoints).IsNotNull();

        var admins = endpoints.MapGroup("/tenants/{tenantId}/admins")
            .WithTags("Identity")
            .RequireAuthorization(IdentityPolicyNames.ManageTenantAdmins);

        admins.MapGet(string.Empty, async (string tenantId, Page? page, PageSize? pageSize, IDispatcher dispatcher, CancellationToken cancellationToken) =>
            (await dispatcher.QueryAsync(new ListTenantAdmins(tenantId, page ?? Page.First, pageSize ?? PageSize.Default), cancellationToken)).ToHttpResult());

        admins.MapPost(string.Empty, async (string tenantId, AdminInvitationRequest request, ClaimsPrincipal user, IDispatcher dispatcher, CancellationToken cancellationToken) =>
        {
            var (email, firstName, lastName) = request;
            var inviteTenantAdmin = new InviteTenantAdmin(tenantId, email, firstName, lastName, Caller.Of(user));
            return (await dispatcher.SendAsync(inviteTenantAdmin, cancellationToken))
                .ToHttpResult(id => TypedResults.Created($"/identity/tenants/{Uri.EscapeDataString(tenantId)}/admins/{id}", id));
        });

        admins.MapPut("/{userId:guid}/role", async (string tenantId, Guid userId, RoleRequest request, ClaimsPrincipal user, IDispatcher dispatcher, CancellationToken cancellationToken) =>
            (await dispatcher.SendAsync(new ChangeTenantAdminRole(tenantId, userId, request.Role, Caller.Of(user)), cancellationToken))
                .ToHttpResult(_ => TypedResults.NoContent()));
    }
}
