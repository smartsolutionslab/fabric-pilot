using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Knowledge.Application;
using FabricPilot.Modules.Knowledge.Infrastructure.Persistence;

namespace FabricPilot.Modules.Knowledge.Infrastructure;

public sealed class KnowledgeUnitOfWork(IServiceProvider services)
    : ModuleUnitOfWork<KnowledgeDbContext>(services), IKnowledgeUnitOfWork;
