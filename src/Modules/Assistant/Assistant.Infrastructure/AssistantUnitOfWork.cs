using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Assistant.Application;
using FabricPilot.Modules.Assistant.Infrastructure.Persistence;

namespace FabricPilot.Modules.Assistant.Infrastructure;

public sealed class AssistantUnitOfWork(IServiceProvider services)
    : ModuleUnitOfWork<AssistantDbContext>(services), IAssistantUnitOfWork;
