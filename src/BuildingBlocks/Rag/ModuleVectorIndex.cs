using System.Globalization;
using FabricPilot.BuildingBlocks.Persistence;
using FabricPilot.BuildingBlocks.Tenancy;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Pgvector;

namespace FabricPilot.BuildingBlocks.Rag;

/// <summary>
/// One module's vectors: the <c>embeddings</c> table in the schema of the module's own DbContext,
/// <typeparamref name="TContext"/>, which the MigrationRunner creates. A record is indexed under its key with the text
/// the embedding was made from, and a search returns the records nearest to a question by cosine distance. Every index
/// and every search sends its text to the embeddings provider. A module uses this only with its own DbContext.
/// Every record belongs to the current tenant, from <see cref="ITenantContext"/> as the module's DbContext reads it: a
/// key is a key within that tenant, a search finds only its records, and without a tenant nothing is found, removed or
/// indexed.
/// </summary>
/// <typeparam name="TContext">The module's DbContext, whose <c>Schema</c> holds the table.</typeparam>
public sealed class ModuleVectorIndex<TContext>(
    [FromKeyedServices(VectorTables.DataSourceKey)] NpgsqlDataSource dataSource,
    IEmbeddingGenerator<string, Embedding<float>> generator,
    ITenantContext tenants)
    where TContext : ModuleDbContext
{
    /// <summary>pgvector's bounds for hnsw.ef_search; its default is the lower one.</summary>
    private const int MinimumBreadth = 40;

    private const int MaximumBreadth = 1000;

    private static readonly string Table = VectorTables.Table(VectorTables.SchemaOf(typeof(TContext)));

    /// <summary>Embeds the text and stores it under the key, replacing what the key held before.</summary>
    public async Task IndexAsync(string key, string text, CancellationToken cancellationToken)
    {
        Ensure.That(key).IsNotNullOrWhiteSpace();
        Ensure.That(text).IsNotNullOrWhiteSpace();

        var tenant = tenants.TenantId ?? throw new InvalidOperationException($"Cannot index '{key}' without a tenant.");
        var embedding = await EmbedAsync(text, cancellationToken);
        await using var command = dataSource.CreateCommand(
            $"INSERT INTO {Table} (tenant_id, key, content, embedding) VALUES ($1, $2, $3, $4) " +
            "ON CONFLICT (tenant_id, key) DO UPDATE SET content = excluded.content, embedding = excluded.embedding");
        command.Parameters.Add(new NpgsqlParameter { Value = tenant });
        command.Parameters.Add(new NpgsqlParameter { Value = key });
        command.Parameters.Add(new NpgsqlParameter { Value = text });
        command.Parameters.Add(new NpgsqlParameter { Value = embedding });
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>Removes the record stored under the key, if there is one.</summary>
    public async Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        Ensure.That(key).IsNotNullOrWhiteSpace();

        if (tenants.TenantId is not { } tenant)
        {
            return;
        }

        await using var command = dataSource.CreateCommand($"DELETE FROM {Table} WHERE tenant_id = $1 AND key = $2");
        command.Parameters.Add(new NpgsqlParameter { Value = tenant });
        command.Parameters.Add(new NpgsqlParameter { Value = key });
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// The <paramref name="count"/> records nearest to the query, nearest first. Their text is untrusted: whoever wrote
    /// a record wrote it, so it reaches a model as data in a delimited user message, never as instructions.
    /// </summary>
    public async Task<IReadOnlyList<RetrievedPassage>> SearchAsync(string query, int count, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNullOrWhiteSpace();

        if (tenants.TenantId is not { } tenant)
        {
            return [];
        }

        var embedding = await EmbedAsync(query, cancellationToken);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // An HNSW scan stops after hnsw.ef_search candidates (40 by default), so a wider search would come back short:
        // widen it to the count, and let the scan go on in order (pgvector's iterative scan) when that is not enough.
        await using (var breadth = new NpgsqlCommand("SELECT set_config('hnsw.ef_search', $1, true), set_config('hnsw.iterative_scan', 'strict_order', true)", connection, transaction))
        {
            breadth.Parameters.Add(new NpgsqlParameter { Value = Math.Clamp(count, MinimumBreadth, MaximumBreadth).ToString(CultureInfo.InvariantCulture) });
            await breadth.ExecuteNonQueryAsync(cancellationToken);
        }

        List<RetrievedPassage> passages = [];
        await using (var search = new NpgsqlCommand($"SELECT key, content, embedding <=> $1 FROM {Table} WHERE tenant_id = $3 ORDER BY embedding <=> $1 LIMIT $2", connection, transaction))
        {
            search.Parameters.Add(new NpgsqlParameter { Value = embedding });
            search.Parameters.Add(new NpgsqlParameter { Value = count });
            search.Parameters.Add(new NpgsqlParameter { Value = tenant });
            await using var reader = await search.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                passages.Add(new RetrievedPassage(reader.GetString(0), reader.GetString(1), reader.GetDouble(2)));
            }
        }

        // The HNSW index holds every tenant's vectors and its scan stops after hnsw.max_scan_tuples, so where other
        // tenants' records lie nearer it can come back short. Then the tenant's records are ranked exactly, read through
        // the key's index (tenant_id leads it); the materialized set keeps the HNSW index out of that ranking.
        if (passages.Count < count)
        {
            passages.Clear();
            await using var exact = new NpgsqlCommand(
                $"WITH tenant AS MATERIALIZED (SELECT key, content, embedding FROM {Table} WHERE tenant_id = $3) " +
                "SELECT key, content, embedding <=> $1 FROM tenant ORDER BY embedding <=> $1, key LIMIT $2",
                connection,
                transaction);
            exact.Parameters.Add(new NpgsqlParameter { Value = embedding });
            exact.Parameters.Add(new NpgsqlParameter { Value = count });
            exact.Parameters.Add(new NpgsqlParameter { Value = tenant });
            await using var reader = await exact.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                passages.Add(new RetrievedPassage(reader.GetString(0), reader.GetString(1), reader.GetDouble(2)));
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return passages;
    }

    private async Task<Vector> EmbedAsync(string text, CancellationToken cancellationToken) =>
        new(await generator.GenerateVectorAsync(text, cancellationToken: cancellationToken));
}
