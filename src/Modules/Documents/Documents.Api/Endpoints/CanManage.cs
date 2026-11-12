using FabricPilot.BuildingBlocks.Api;
using FabricPilot.Modules.Documents.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FabricPilot.Modules.Documents.Api.Endpoints;

/// <summary>
/// 200 when the caller holds the policy documents.manage, 403 when not: the sample of an endpoint naming one of
/// its module's policies, which the integration tests prove the policy on. It stays out of the OpenAPI document, so a
/// module added after generation leaves the frontend's copy of it current.
/// </summary>
public sealed class CanManage : IEndpoint
{
    public void Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/permissions/manage", () => TypedResults.Ok())
            .RequireAuthorization(DocumentsPolicyNames.Manage)
            .ExcludeFromDescription();
}
