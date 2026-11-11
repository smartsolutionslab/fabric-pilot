using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Tickets.Application;
using FabricPilot.Modules.Tickets.Infrastructure.Persistence;

namespace FabricPilot.Modules.Tickets.Infrastructure;

public sealed class TicketsUnitOfWork(IServiceProvider services)
    : ModuleUnitOfWork<TicketsDbContext>(services), ITicketsUnitOfWork;
