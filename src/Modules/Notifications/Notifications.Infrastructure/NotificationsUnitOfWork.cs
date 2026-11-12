using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Notifications.Application;
using FabricPilot.Modules.Notifications.Infrastructure.Persistence;

namespace FabricPilot.Modules.Notifications.Infrastructure;

public sealed class NotificationsUnitOfWork(IServiceProvider services)
    : ModuleUnitOfWork<NotificationsDbContext>(services), INotificationsUnitOfWork;
