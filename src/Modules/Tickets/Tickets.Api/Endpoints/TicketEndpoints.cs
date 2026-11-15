using System.Security.Claims;
using FabricPilot.BuildingBlocks.Api;
using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Tickets.Application;
using FabricPilot.Modules.Tickets.Application.Commands.AssignTicket;
using FabricPilot.Modules.Tickets.Application.Commands.ChangeTicketStatus;
using FabricPilot.Modules.Tickets.Application.Commands.RaiseTicket;
using FabricPilot.Modules.Tickets.Application.DTOs;
using FabricPilot.Modules.Tickets.Application.Queries.DownloadAttachment;
using FabricPilot.Modules.Tickets.Application.Queries.GetStaffTicket;
using FabricPilot.Modules.Tickets.Application.Queries.GetTicket;
using FabricPilot.Modules.Tickets.Application.Queries.ListTriageTickets;
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
/// for staff, the triage queue of every tenant's tickets and each of them, read across tenants, and assigning a ticket
/// or changing its status in the tenant the route names.
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

        // The triage queue, every tenant's rows: staff, who belong to no tenant, read it and write nothing.
        tickets.MapGet("/all-tenants", ListQueueAsync)
            .RequireAuthorization(TicketsPolicyNames.ReadAllTenants)
            .AcrossTenants();

        // One of them, with its description and tenant, for staff to answer and triage: read only, as the queue.
        tickets.MapGet("/all-tenants/{ticketId:guid}", async (Guid ticketId, IDispatcher dispatcher, CancellationToken cancellationToken) =>
            (await dispatcher.QueryAsync(new GetStaffTicket(ticketId), cancellationToken)).ToHttpResult())
            .RequireAuthorization(TicketsPolicyNames.ReadAllTenants)
            .AcrossTenants();

        // Staff triage a ticket in its own tenant, which the route names: the ticket is found among that tenant's only.
        tickets.MapPut("/{tenant}/{ticketId:guid}/status", ChangeStatusAsync)
            .RequireAuthorization(TicketsPolicyNames.WriteForTenants)
            .ForTenant();
        tickets.MapPut("/{tenant}/{ticketId:guid}/assignee", AssignAsync)
            .RequireAuthorization(TicketsPolicyNames.WriteForTenants)
            .ForTenant();
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

    /// <summary>Newest first unless the caller names a field, as the customers' list; every filter left out shows every ticket.</summary>
    private static async Task<Results<Ok<PagedResult<QueuedTicketDto>>, ProblemHttpResult>> ListQueueAsync(
        Page? page,
        PageSize? pageSize,
        TicketSortField? sortBy,
        SortDirection? sortDirection,
        string? tenant,
        TicketStatus? status,
        TicketPriority? priority,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var sorting = new Sorting<TicketSortField>(
            sortBy ?? ListTickets.NewestFirst.Field,
            sortDirection ?? ListTickets.NewestFirst.Direction);
        var filter = new TicketQueueFilter(
            string.IsNullOrEmpty(tenant) ? Option<string>.None : Option<string>.Some(tenant),
            status is { } named ? Option<TicketStatus>.Some(named) : Option<TicketStatus>.None,
            priority is { } urgency ? Option<TicketPriority>.Some(urgency) : Option<TicketPriority>.None);
        var listTriageTickets = new ListTriageTickets(page ?? Page.First, pageSize ?? PageSize.Default, sorting, filter);
        return (await dispatcher.QueryAsync(listTriageTickets, cancellationToken)).ToHttpResult();
    }

    /// <summary>
    /// The handler runs in the route's tenant (<c>ForTenant()</c>, which checked it), so the command does not carry it;
    /// <paramref name="tenant"/> is bound only for the OpenAPI document and the generated client to name the route's parameter.
    /// </summary>
    private static async Task<Results<NoContent, ProblemHttpResult>> ChangeStatusAsync(
        string tenant,
        Guid ticketId,
        TicketStatusChange change,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Ensure.That(change).IsNotNull();

        var result = await dispatcher.SendAsync(new ChangeTicketStatus(ticketId, change.Status), cancellationToken);
        return result.ToHttpResult(_ => TypedResults.NoContent());
    }

    /// <summary>In the route's tenant, as <see cref="ChangeStatusAsync"/>.</summary>
    private static async Task<Results<NoContent, ProblemHttpResult>> AssignAsync(
        string tenant,
        Guid ticketId,
        TicketAssignment assignment,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Ensure.That(assignment).IsNotNull();

        var result = await dispatcher.SendAsync(new AssignTicket(ticketId, assignment.Assignee), cancellationToken);
        return result.ToHttpResult(_ => TypedResults.NoContent());
    }

    private static async Task<Results<Created<Guid>, ProblemHttpResult>> RaiseAsync(
        [FromForm] string subject,
        [FromForm] string description,
        [FromForm] string priority,
        IFormFileCollection attachments,
        ClaimsPrincipal user,
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

            var result = await dispatcher.SendAsync(new RaiseTicket(subject, description, priority, CustomerClaims.Of(user), files), cancellationToken);
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
