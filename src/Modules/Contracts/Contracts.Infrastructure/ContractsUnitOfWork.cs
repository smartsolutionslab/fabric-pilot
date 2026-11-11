using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Contracts.Application;
using FabricPilot.Modules.Contracts.Infrastructure.Persistence;

namespace FabricPilot.Modules.Contracts.Infrastructure;

public sealed class ContractsUnitOfWork(IServiceProvider services)
    : ModuleUnitOfWork<ContractsDbContext>(services), IContractsUnitOfWork;
