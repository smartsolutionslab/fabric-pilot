using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Tickets.Domain.Tickets;

namespace FabricPilot.Modules.Tickets.Application.Commands.RaiseTicket;

/// <summary>
/// Checks the ticket's rules before anything is stored, then uploads each file and saves the ticket last: a ticket
/// is never saved without its files. A save that fails after the uploads leaves those files behind, unreferenced.
/// </summary>
public sealed class RaiseTicketHandler(ITicketStore tickets, IBlobStore blobs, TimeProvider time)
    : ICommandHandler<RaiseTicket, Result<Guid, ApiError>>
{
    public async Task<Result<Guid, ApiError>> HandleAsync(RaiseTicket command, CancellationToken cancellationToken)
    {
        Ensure.That(command).IsNotNull();

        var (subject, description, priorityName, attachments) = command;
        if (!Enum.GetNames<TicketPriority>().Contains(priorityName, StringComparer.Ordinal))
        {
            return Failure(TicketsFailures.UnknownPriority(priorityName));
        }

        var files = attachments.Select(file => (Id: AttachmentId.New(), File: file)).ToList();
        var raised = Ticket.Raise(
            TicketId.New(),
            subject,
            description,
            Enum.Parse<TicketPriority>(priorityName),
            [.. files.Select(file => new AttachmentFile(file.Id, file.File.FileName, file.File.ContentType, file.File.Size))],
            time.GetUtcNow());
        if (raised.IsFailure)
        {
            return Failure(TicketsFailures.From(raised.Error));
        }

        var ticket = raised.Value;
        foreach (var (id, file) in files)
        {
            await blobs.UploadAsync(AttachmentBlobs.Name(ticket.Id, id), file.Content, cancellationToken);
        }

        tickets.Add(ticket);
        await tickets.SaveAsync(cancellationToken);
        return Success(ticket.Id.Value);
    }
}
