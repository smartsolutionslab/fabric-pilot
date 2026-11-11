using FabricPilot.BuildingBlocks.Identity;
using FabricPilot.BuildingBlocks.Messaging;

namespace FabricPilot.Modules.Dashboards.Application.Commands.RemoveDashboard;

public sealed class RemoveDashboardHandler(IDashboardStore dashboards, ICurrentUser user) : ICommandHandler<RemoveDashboard, Result<Unit, ApiError>>
{
    public async Task<Result<Unit, ApiError>> HandleAsync(RemoveDashboard command, CancellationToken cancellationToken)
    {
        Ensure.That(command).IsNotNull();

        if (user.Subject is not { } owner)
        {
            return Failure(DashboardsFailures.NotSignedIn());
        }

        var found = await dashboards.FindAsync(command.Id, owner, cancellationToken);
        if (!found.HasValue)
        {
            return Failure(DashboardsFailures.NotFound(command.Id));
        }

        dashboards.Remove(found.Value);
        await dashboards.SaveAsync(cancellationToken);
        return Success(Unit.Value);
    }
}
