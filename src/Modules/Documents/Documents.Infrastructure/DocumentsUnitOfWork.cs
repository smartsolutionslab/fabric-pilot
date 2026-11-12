using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Documents.Application;
using FabricPilot.Modules.Documents.Infrastructure.Persistence;

namespace FabricPilot.Modules.Documents.Infrastructure;

public sealed class DocumentsUnitOfWork(IServiceProvider services)
    : ModuleUnitOfWork<DocumentsDbContext>(services), IDocumentsUnitOfWork;
