using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Identity.Application.DTOs;

namespace FabricPilot.Modules.Identity.Application.Queries.ListTenantAdmins;

/// <summary>Staff list the admins of the tenant the route names, by username, a page at a time.</summary>
public sealed record ListTenantAdmins(string Tenant, Page Page, PageSize PageSize) : IQuery<Result<PagedResult<TenantUserDto>, ApiError>>;
