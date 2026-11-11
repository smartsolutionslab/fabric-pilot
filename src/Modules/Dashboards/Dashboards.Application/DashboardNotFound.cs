using System.Net;

namespace FabricPilot.Modules.Dashboards.Application;

public sealed record DashboardNotFound(Guid Id) : ApiError("dashboards.dashboard-not-found", $"You have no dashboard {Id}.", HttpStatusCode.NotFound);
