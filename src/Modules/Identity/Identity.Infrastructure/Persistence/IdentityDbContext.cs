using FabricPilot.BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FabricPilot.Modules.Identity.Infrastructure.Persistence;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options, IServiceProvider services)
    : ModuleDbContext(options, services, Schema)
{
    /// <summary>The module's tables and its migrations history live in this schema.</summary>
    public const string Schema = "identity";
}
