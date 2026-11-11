using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Dashboards.Application;
using FabricPilot.Modules.Dashboards.Infrastructure.Persistence;

namespace FabricPilot.Modules.Dashboards.Infrastructure;

public sealed class DashboardsUnitOfWork(IServiceProvider services)
    : ModuleUnitOfWork<DashboardsDbContext>(services), IDashboardsUnitOfWork;
