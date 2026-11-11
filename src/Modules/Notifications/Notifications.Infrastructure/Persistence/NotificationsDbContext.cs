using FabricPilot.BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FabricPilot.Modules.Notifications.Infrastructure.Persistence;

public sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options, IServiceProvider services)
    : ModuleDbContext(options, services, Schema)
{
    /// <summary>The module's tables and its migrations history live in this schema.</summary>
    public const string Schema = "notifications";
}
