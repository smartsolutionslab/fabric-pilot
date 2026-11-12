using FabricPilot.BuildingBlocks.Rag;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Microsoft.Extensions.DependencyInjection;

public static class VectorIndexRegistration
{
    private const string ConnectionStringName = "database";

    /// <summary>
    /// Registers <see cref="ModuleVectorIndex{TContext}"/> for every module, over a data source of the connection
    /// string <c>database</c> that knows the <c>vector</c> type. The connection string is read when an index is first
    /// needed; registering connects to nothing.
    /// An index is scoped, as the tenant source it reads is.
    /// </summary>
    public static IServiceCollection AddModuleVectorIndexes(this IServiceCollection services, IConfiguration configuration)
    {
        Ensure.That(configuration).IsNotNull();

        return services
            .AddKeyedSingleton(VectorTables.DataSourceKey, (_, _) => VectorTables.DataSource(ConnectionString(configuration)))
            .AddScoped(typeof(ModuleVectorIndex<>));
    }

    /// <summary>
    /// Creates the vector extension and each module's <c>embeddings</c> table, in the schema of its DbContext. A step
    /// of the MigrationRunner, after the modules' migrations, so hosts never create tables.
    /// </summary>
    public static Task CreateVectorTablesAsync(this IServiceProvider services, IEnumerable<Type> contexts, CancellationToken cancellationToken = default)
    {
        Ensure.That(contexts).IsNotNull();

        return VectorTables.CreateAsync(
            ConnectionString(services.GetRequiredService<IConfiguration>()),
            contexts.Select(VectorTables.SchemaOf),
            cancellationToken);
    }

    private static string ConnectionString(IConfiguration configuration) =>
        configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is missing; Aspire provides it.");
}
