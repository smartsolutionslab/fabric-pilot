using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Tickets.Application.DTOs;

namespace FabricPilot.Modules.Tickets.Application.Queries.GetStaffTicket;

/// <summary>One ticket of any tenant, for staff to read across tenants: with its tenant, status and assignee.</summary>
public sealed record GetStaffTicket(Guid TicketId) : IQuery<Result<StaffTicketDto, ApiError>>;
