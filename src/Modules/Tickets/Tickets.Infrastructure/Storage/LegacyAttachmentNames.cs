using Azure.Storage.Blobs;
using FabricPilot.BuildingBlocks.Persistence;
using FabricPilot.Modules.Tickets.Domain.Tickets;
using FabricPilot.Modules.Tickets.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FabricPilot.Modules.Tickets.Infrastructure.Storage;

/// <summary>
/// Moves the attachments stored before their names carried the tenant, <c>{ticket}/{attachment}</c>, under the tenant
/// of the ticket that names them, where <see cref="AzureBlobStore"/> reads them. A blob no ticket names stays where it
/// is: nothing serves it, under either name.
/// </summary>
public sealed class LegacyAttachmentNames(TicketsDbContext tickets, IConfiguration configuration) : IDataMigration
{
    private const string BlobsConnectionName = "blobs";

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var service = BlobServiceClients.Create(configuration.GetConnectionString(BlobsConnectionName)
            ?? throw new InvalidOperationException($"Connection string '{BlobsConnectionName}' is missing; Aspire provides it."));
        var container = service.GetBlobContainerClient(AzureBlobStore.Container);
        if (!await container.ExistsAsync(cancellationToken))
        {
            return;
        }

        List<(string Name, TicketId Ticket)> legacy = [];
        await foreach (var blob in container.GetBlobsAsync(cancellationToken: cancellationToken))
        {
            if (blob.Name.Split('/') is [var ticket, var attachment] && Guid.TryParse(ticket, out var ticketId) && Guid.TryParse(attachment, out _))
            {
                legacy.Add((blob.Name, new TicketId(ticketId)));
            }
        }

        if (legacy.Count == 0)
        {
            return;
        }

        var tenants = await TenantsOfAsync([.. legacy.Select(blob => blob.Ticket).Distinct()], cancellationToken);
        foreach (var (name, ticket) in legacy.Where(blob => tenants.ContainsKey(blob.Ticket)))
        {
            await MoveAsync(container, name, AzureBlobStore.TenantName(tenants[ticket], name), cancellationToken);
        }
    }

    private static async Task MoveAsync(BlobContainerClient container, string from, string to, CancellationToken cancellationToken)
    {
        var source = container.GetBlobClient(from);
        await using (var content = await source.OpenReadAsync(cancellationToken: cancellationToken))
        {
            await container.GetBlobClient(to).UploadAsync(content, overwrite: true, cancellationToken);
        }

        await source.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// The tenant of each ticket, read across every tenant's rows: the runner works for no tenant, and only reads them.
    /// </summary>
    private async Task<Dictionary<TicketId, string>> TenantsOfAsync(List<TicketId> ids, CancellationToken cancellationToken)
    {
        var rows = await tickets.Set<Ticket>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(ticket => ids.Contains(ticket.Id))
            .Select(ticket => new { ticket.Id, Tenant = EF.Property<string>(ticket, "TenantId") })
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(row => row.Id, row => row.Tenant);
    }
}
