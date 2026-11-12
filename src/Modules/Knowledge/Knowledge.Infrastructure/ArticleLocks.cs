using FabricPilot.BuildingBlocks.Rag;
using FabricPilot.Modules.Knowledge.Application;
using FabricPilot.Modules.Knowledge.Domain.Articles;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace FabricPilot.Modules.Knowledge.Infrastructure;

/// <summary>
/// A PostgreSQL session advisory lock per article, on a connection of its own that is held until the lock is released,
/// so every instance of the host waits for the same lock.
/// </summary>
public sealed class ArticleLocks([FromKeyedServices(VectorTables.DataSourceKey)] NpgsqlDataSource dataSource) : IArticleLocks
{
    public async Task<IAsyncDisposable> AcquireAsync(ArticleId article, CancellationToken cancellationToken)
    {
        Ensure.That(article).IsNotNull();

        var key = KeyOf(article);
        var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = new NpgsqlCommand("SELECT pg_advisory_lock($1)", connection);
            command.Parameters.Add(new NpgsqlParameter { Value = key });
            await command.ExecuteNonQueryAsync(cancellationToken);
            return new Held(connection, key);
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    /// <summary>The article's Guid folded into the 64 bits an advisory lock takes.</summary>
    private static long KeyOf(ArticleId article)
    {
        var bytes = article.Value.ToByteArray();
        return BitConverter.ToInt64(bytes, 0) ^ BitConverter.ToInt64(bytes, 8);
    }

    private sealed class Held(NpgsqlConnection connection, long key) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            // Closing the session releases its advisory locks too; unlocking first returns the pooled connection clean.
            await using (connection)
            {
                await using var command = new NpgsqlCommand("SELECT pg_advisory_unlock($1)", connection);
                command.Parameters.Add(new NpgsqlParameter { Value = key });
                await command.ExecuteNonQueryAsync();
            }
        }
    }
}
