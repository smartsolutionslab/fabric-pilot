using FabricPilot.BuildingBlocks.Messaging;

namespace FabricPilot.Modules.Dashboards.Application.Commands.RemoveDashboard;

public sealed record RemoveDashboard(Guid Id) : ICommand<Result<Unit, ApiError>>;
