using System.Reflection;
using FabricPilot.BuildingBlocks.Modules;
using FabricPilot.BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class PersistenceRegistration
{
    private const string ConnectionStringName = "database";

    /// <summary>Registers the module's DbContext, if its Infrastructure project has one, with a health check.</summary>
    public static IServiceCollection AddModuleDbContext<TModule>(this IServiceCollection services, IConfiguration configuration)
        where TModule : IModule
    {
        var context = ModuleAssemblies.Of<TModule>("Infrastructure").GetTypes()
            .SingleOrDefault(type => type is { IsClass: true, IsAbstract: false } && typeof(ModuleDbContext).IsAssignableFrom(type));

        return context is null ? services : services.AddModuleDbContext(context, configuration);
    }

    /// <summary>Registers one module DbContext type, with a health check.</summary>
    public static IServiceCollection AddModuleDbContext(this IServiceCollection services, Type context, IConfiguration configuration)
    {
        Ensure.That(context).IsNotNull();
        Ensure.That(configuration).IsNotNull();

        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is missing; Aspire provides it.");
        var schema = (string)(context.GetField("Schema")?.GetValue(null)
            ?? throw new InvalidOperationException($"{context.Name} declares no Schema constant."));

        typeof(PersistenceRegistration)
            .GetMethod(nameof(Register), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(context)
            .Invoke(null, [services, connectionString, schema]);
        return services;
    }

    private static void Register<TContext>(IServiceCollection services, string connectionString, string schema)
        where TContext : ModuleDbContext
    {
        services.AddDbContext<TContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", schema));
        });
        services.AddHealthChecks().AddDbContextCheck<TContext>(typeof(TContext).Name);
    }
}
