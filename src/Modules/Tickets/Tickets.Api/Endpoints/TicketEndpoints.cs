using FabricPilot.BuildingBlocks.Api;
using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Tickets.Application.Commands.RaiseTicket;
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

/// <summary>The caller's tenant's tickets: raising one with its files, listing them, reading one and downloading its files.</summary>
public sealed class TicketEndpoints : IEndpoint
{
    /// <summary>Every attachment at its largest, and room for the subject, the description and the form's own framing.</summary>
    private const long LargestRequest = (Ticket.MaximumAttachments * Attachment.MaximumSize) + (1024 * 1024);

    public void Map(IEndpointRouteBuilder endpoints)
    {
        Ensure.That(endpoints).IsNotNull();

        // Callers authenticate with a bearer token, never a cookie, so a forged cross-site form carries no credentials:
        // the antiforgery check form endpoints get by default guards against nothing here.
        endpoints.MapPost("/tickets", RaiseAsync)
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(LargestRequest))
            .WithFormOptions(multipartBodyLengthLimit: LargestRequest);
        endpoints.MapGet("/tickets", async (Page? page, PageSize? pageSize, IDispatcher dispatcher, CancellationToken cancellationToken) =>
            (await dispatcher.QueryAsync(new ListTickets(page ?? Page.First, pageSize ?? PageSize.Default), cancellationToken)).ToHttpResult());
        endpoints.MapGet("/tickets/{ticketId:guid}", async (Guid ticketId, IDispatcher dispatcher, CancellationToken cancellationToken) =>
            (await dispatcher.QueryAsync(new GetTicket(ticketId), cancellationToken)).ToHttpResult());
        endpoints.MapGet("/tickets/{ticketId:guid}/attachments/{attachmentId:guid}", async (Guid ticketId, Guid attachmentId, IDispatcher dispatcher, CancellationToken cancellationToken) =>
            (await dispatcher.QueryAsync(new DownloadAttachment(ticketId, attachmentId), cancellationToken))
                // Always a download, never shown inline: an uploaded page must not run in the portal's origin.
                .ToHttpResult(download => TypedResults.Stream(download.Content, "application/octet-stream", download.FileName)))
            .Produces<Stream>(StatusCodes.Status200OK, "application/octet-stream");
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
