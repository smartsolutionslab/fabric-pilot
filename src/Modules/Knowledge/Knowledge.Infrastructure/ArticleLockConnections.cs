using Microsoft.Extensions.Configuration;
using Npgsql;

namespace FabricPilot.Modules.Knowledge.Infrastructure;

/// <summary>
/// The connections the article locks hold, from a data source of their own of <see cref="ArticleLocks.Capacity"/>
/// connections: a held lock never takes a connection the vector indexes or any other database work needs. A change takes
/// a slot without waiting; when every slot is taken, it gets none. One per host.
/// </summary>
public sealed class ArticleLockConnections(IConfiguration configuration) : IAsyncDisposable
{
    private const string ConnectionStringName = "database";

    private readonly SemaphoreSlim slots = new(ArticleLocks.Capacity, ArticleLocks.Capacity);

    private readonly Lazy<NpgsqlDataSource> dataSource = new(() =>
    {
        var connectionString = new NpgsqlConnectionStringBuilder(configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is missing; Aspire provides it."))
        {
            MaxPoolSize = ArticleLocks.Capacity,
            ApplicationName = "article-locks",
        };
        return NpgsqlDataSource.Create(connectionString);
    });

    /// <summary>An open connection of the lock pool, or None at once when every one is in use; dispose the lease to give it back.</summary>
    public async Task<Option<Lease>> TryOpenAsync(CancellationToken cancellationToken)
    {
        if (!slots.Wait(0, CancellationToken.None))
        {
            return Option<Lease>.None;
        }

        try
        {
            var connection = await dataSource.Value.OpenConnectionAsync(cancellationToken);
            return Option<Lease>.Some(new Lease(connection, slots));
        }
        catch
        {
            slots.Release();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (dataSource.IsValueCreated)
        {
            await dataSource.Value.DisposeAsync();
        }

        slots.Dispose();
    }

    /// <summary>One connection of the lock pool; disposing it closes the connection and frees its slot.</summary>
    public sealed class Lease(NpgsqlConnection connection, SemaphoreSlim slots) : IAsyncDisposable
    {
        public NpgsqlConnection Connection => connection;

        public async ValueTask DisposeAsync()
        {
            try
            {
                await connection.DisposeAsync();
            }
            finally
            {
                slots.Release();
            }
        }
    }
}
