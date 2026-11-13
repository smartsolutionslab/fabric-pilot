using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Identity.Application;
using FabricPilot.Modules.Identity.Infrastructure.Persistence;

namespace FabricPilot.Modules.Identity.Infrastructure;

public sealed class IdentityUnitOfWork(IServiceProvider services)
    : ModuleUnitOfWork<IdentityDbContext>(services), IIdentityUnitOfWork;
