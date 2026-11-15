using FabricPilot.BuildingBlocks.Api;
using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Tickets.Application;
using FabricPilot.Modules.Tickets.Application.Commands.RaiseTicket;
using FabricPilot.Modules.Tickets.Application.DTOs;
using FabricPilot.Modules.Tickets.Application.Queries.DownloadAttachment;
using FabricPilot.Modules.Tickets.Application.Queries.GetTicket;
using FabricPilot.Modules.Tickets.Application.Queries.ListTickets;
using FabricPilot.Modules.Tickets.Domain.Tickets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FabricPilot.Modules.Tickets.Api.Endpoints;

/// <summary>
/// The caller's tenant's tickets: raising one with its files, listing them, reading one and downloading its files; and,
/// for staff, every tenant's tickets and each of them, read only.
/// </summary>
public sealed class TicketEndpoints : IEndpoint
{
    /// <summary>Every attachment at its largest, and room for the subject, the description and the form's own framing.</summary>
    private const long LargestRequest = (Ticket.MaximumAttachments * Attachment.MaximumSize) + (1024 * 1024);

    public void Map(IEndpointRouteBuilder endpoints)
    {
        Ensure.That(endpoints).IsNotNull();

        // One OpenAPI tag for the module's endpoints: the generated client refetches what a tag provides when a request
        // with the same tag changes it, so a raised ticket shows up in the list.
        var tickets = endpoints.MapGroup("/tickets").WithTags("Tickets");

        // Callers authenticate with a bearer token, never a cookie, so a forged cross-site form carries no credentials:
        // the antiforgery check form endpoints get by default guards against nothing here.
        tickets.MapPost(string.Empty, RaiseAsync)
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(LargestRequest))
            .WithFormOptions(multipartBodyLengthLimit: LargestRequest)
            .RequireAuthorization(TicketsPolicyNames.RaiseAndRead);
        tickets.MapGet(string.Empty, ListAsync)
            .RequireAuthorization(TicketsPolicyNames.RaiseAndRead);

        // The same list, every tenant's rows: staff, who belong to no tenant, read it and write nothing.
        tickets.MapGet("/all-tenants", ListAsync)
            .RequireAuthorization(TicketsPolicyNames.ReadAllTenants)
            .AcrossTenants();

        // One of them, with its description, for staff to answer: read only, as the list.
        tickets.MapGet("/all-tenants/{ticketId:guid}", async (Guid ticketId, IDispatcher dispatcher, CancellationToken cancellationToken) =>
            (await dispatcher.QueryAsync(new GetTicket(ticketId), cancellationToken)).ToHttpResult())
            .RequireAuthorization(TicketsPolicyNames.ReadAllTenants)
            .AcrossTenants();
        tickets.MapGet("/{ticketId:guid}", async (Guid ticketId, IDispatcher dispatcher, CancellationToken cancellationToken) =>
            (await dispatcher.QueryAsync(new GetTicket(ticketId), cancellationToken)).ToHttpResult())
            .RequireAuthorization(TicketsPolicyNames.RaiseAndRead);
        tickets.MapGet("/{ticketId:guid}/attachments/{attachmentId:guid}", async (Guid ticketId, Guid attachmentId, IDispatcher dispatcher, CancellationToken cancellationToken) =>
            (await dispatcher.QueryAsync(new DownloadAttachment(ticketId, attachmentId), cancellationToken))
                // Always a download, never shown inline: an uploaded page must not run in the portal's origin.
                .ToHttpResult(download => TypedResults.Stream(download.Content, "application/octet-stream", download.FileName)))
            .Produces<Stream>(StatusCodes.Status200OK, "application/octet-stream")
            .RequireAuthorization(TicketsPolicyNames.RaiseAndRead);
    }

    /// <summary>Newest first unless the caller names a field; a named field sorts descending unless the caller says otherwise.</summary>
    private static async Task<Results<Ok<PagedResult<TicketSummaryDto>>, ProblemHttpResult>> ListAsync(
        Page? page,
        PageSize? pageSize,
        TicketSortField? sortBy,
        SortDirection? sortDirection,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var sorting = new Sorting<TicketSortField>(
            sortBy ?? ListTickets.NewestFirst.Field,
            sortDirection ?? ListTickets.NewestFirst.Direction);
        var listTickets = new ListTickets(page ?? Page.First, pageSize ?? PageSize.Default, sorting);
        return (await dispatcher.QueryAsync(listTickets, cancellationToken)).ToHttpResult();
    }

    private static async Task<Results<Created<Guid>, ProblemHttpResult>> RaiseAsync(
        [FromForm] string subject,
        [FromForm] string description,
        [FromForm] string priority,
        IFormFileCollection attachments,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        List<Stream> opened = [];
        try
        {
            List<NewAttachment> files = [];
            foreach (var file in attachments)
            {
                var content = file.OpenReadStream();
                opened.Add(content);
                files.Add(new NewAttachment(file.FileName, file.ContentType, file.Length, content));
            }

            var result = await dispatcher.SendAsync(new RaiseTicket(subject, description, priority, files), cancellationToken);
            return result.ToHttpResult(id => TypedResults.Created($"/tickets/tickets/{id}", id));
        }
        finally
        {
            foreach (var content in opened)
            {
                await content.DisposeAsync();
            }
        }
    }
}
