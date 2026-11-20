using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Identity.Application.DTOs;

namespace FabricPilot.Modules.Identity.Application.Queries.ListTenantUsers;

/// <summary>The caller's tenant's users, by username, a page at a time.</summary>
public sealed record ListTenantUsers(Page Page, PageSize PageSize) : IQuery<Result<PagedResult<TenantUserDto>, ApiError>>;
