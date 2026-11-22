using System.Reflection;
using FabricPilot.BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;

// Applies every module's migrations, then its data migrations, then exits; the AppHost starts the host only after this completes.
var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

var types = Directory.EnumerateFiles(AppContext.BaseDirectory, "FabricPilot.Modules.*.Infrastructure.dll")
    .Order(StringComparer.Ordinal)
    .Select(Assembly.LoadFrom)
    .SelectMany(assembly => assembly.GetTypes())
    .Where(type => type is { IsClass: true, IsAbstract: false })
    .ToList();
var contexts = types.Where(typeof(ModuleDbContext).IsAssignableFrom).ToList();
var dataMigrations = types.Where(typeof(IDataMigration).IsAssignableFrom).ToList();

foreach (var context in contexts)
{
    builder.Services.AddModuleDbContext(context, builder.Configuration);
}

using var host = builder.Build();
foreach (var context in contexts)
{
    await using var scope = host.Services.CreateAsyncScope();
    await ((ModuleDbContext)scope.ServiceProvider.GetRequiredService(context)).MigrateModuleAsync();
}

await host.Services.CreateVectorTablesAsync(contexts);

foreach (var dataMigration in dataMigrations)
{
    await using var scope = host.Services.CreateAsyncScope();
    await ((IDataMigration)ActivatorUtilities.CreateInstance(scope.ServiceProvider, dataMigration)).RunAsync(CancellationToken.None);
}
