using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Billing.Application;
using FabricPilot.Modules.Billing.Infrastructure.Persistence;

namespace FabricPilot.Modules.Billing.Infrastructure;

public sealed class BillingUnitOfWork(IServiceProvider services)
    : ModuleUnitOfWork<BillingDbContext>(services), IBillingUnitOfWork;
