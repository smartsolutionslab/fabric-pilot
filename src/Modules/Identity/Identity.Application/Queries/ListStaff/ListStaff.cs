using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Identity.Application.DTOs;

namespace FabricPilot.Modules.Identity.Application.Queries.ListStaff;

/// <summary>Staff list the realm's staff by user name, a page at a time, to pick an assignee from.</summary>
public sealed record ListStaff(Page Page, PageSize PageSize) : IQuery<Result<PagedResult<StaffMemberDto>, ApiError>>;
