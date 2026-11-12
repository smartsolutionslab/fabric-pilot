using FabricPilot.Modules.Knowledge.Application;
using FabricPilot.Modules.Knowledge.Domain.Articles;
using Npgsql;

namespace FabricPilot.Modules.Knowledge.Infrastructure;

/// <summary>
/// A PostgreSQL session advisory lock per article, so every instance of the host sees the same lock. It is tried, never
/// waited for, on a connection of the locks' own pool (<see cref="ArticleLockConnections"/>): a held lock keeps one of
/// those, never one the vector indexes need, and a change that finds the lock or the pool taken is refused at once.
/// </summary>
public sealed class ArticleLocks(ArticleLockConnections connections) : IArticleLocks
{
    /// <summary>How many article changes one host runs at once; one more is refused until one of them is done.</summary>
    public const int Capacity = 10;

    public async Task<Option<IAsyncDisposable>> TryAcquireAsync(ArticleId article, CancellationToken cancellationToken)
    {
        Ensure.That(article).IsNotNull();

        var opened = await connections.TryOpenAsync(cancellationToken);
        if (!opened.HasValue)
        {
            return Option<IAsyncDisposable>.None;
        }

        var lease = opened.Value;
        var key = KeyOf(article);
        try
        {
            await using var command = new NpgsqlCommand("SELECT pg_try_advisory_lock($1)", lease.Connection);
            command.Parameters.Add(new NpgsqlParameter { Value = key });
            if (await command.ExecuteScalarAsync(cancellationToken) is true)
            {
                return Option<IAsyncDisposable>.Some(new Held(lease, key));
            }
        }
        catch
        {
            await lease.DisposeAsync();
            throw;
        }

        await lease.DisposeAsync();
        return Option<IAsyncDisposable>.None;
    }

    /// <summary>The article's Guid folded into the 64 bits an advisory lock takes.</summary>
    private static long KeyOf(ArticleId article)
    {
        var bytes = article.Value.ToByteArray();
        return BitConverter.ToInt64(bytes, 0) ^ BitConverter.ToInt64(bytes, 8);
    }

    private sealed class Held(ArticleLockConnections.Lease lease, long key) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            // Closing the session releases its advisory locks too; unlocking first returns the pooled connection clean.
            await using (lease)
            {
                await using var command = new NpgsqlCommand("SELECT pg_advisory_unlock($1)", lease.Connection);
                command.Parameters.Add(new NpgsqlParameter { Value = key });
                await command.ExecuteNonQueryAsync();
            }
        }
    }
}
