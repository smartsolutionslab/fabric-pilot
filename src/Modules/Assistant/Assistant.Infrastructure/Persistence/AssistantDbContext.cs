using FabricPilot.BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FabricPilot.Modules.Assistant.Infrastructure.Persistence;

public sealed class AssistantDbContext(DbContextOptions<AssistantDbContext> options, IServiceProvider services)
    : ModuleDbContext(options, services, Schema)
{
    /// <summary>The module's tables and its migrations history live in this schema.</summary>
    public const string Schema = "assistant";
}
