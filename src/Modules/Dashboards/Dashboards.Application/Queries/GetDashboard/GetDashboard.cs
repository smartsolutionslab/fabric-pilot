using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Dashboards.Application.DTOs;

namespace FabricPilot.Modules.Dashboards.Application.Queries.GetDashboard;

public sealed record GetDashboard(Guid Id) : IQuery<Result<DashboardDto, ApiError>>;
