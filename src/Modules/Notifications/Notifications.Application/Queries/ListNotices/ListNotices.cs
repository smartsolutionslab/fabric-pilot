using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Notifications.Application.DTOs;

namespace FabricPilot.Modules.Notifications.Application.Queries.ListNotices;

/// <summary>One page of the recipient's notices in the current tenant, newest first.</summary>
/// <param name="Recipient">The Keycloak user id (<c>sub</c>) of the signed-in customer, never one the caller names.</param>
public sealed record ListNotices(string Recipient, Page Page, PageSize PageSize)
    : IQuery<Result<PagedResult<NoticeDto>, ApiError>>;
