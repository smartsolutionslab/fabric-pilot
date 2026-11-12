using FabricPilot.BuildingBlocks.Domain;
using FabricPilot.Modules.Tickets.Domain.Tickets.Events;

namespace FabricPilot.Modules.Tickets.Domain.Tickets;

/// <summary>A customer's request for help: what it is about, how urgent it is, and the files that show it.</summary>
public sealed class Ticket : AggregateRoot<TicketId>
{
    public const int MaximumSubjectLength = 200;

    public const int MaximumDescriptionLength = 4000;

    public const int MaximumAttachments = 5;

    private readonly List<Attachment> attachments = [];

    private Ticket(TicketId id, string subject, string description, TicketPriority priority, DateTimeOffset raisedAt)
        : base(id)
    {
        Subject = subject;
        Description = description;
        Priority = priority;
        RaisedAt = raisedAt;
    }

    public string Subject { get; private set; }

    public string Description { get; private set; }

    public TicketPriority Priority { get; private set; }

    public DateTimeOffset RaisedAt { get; private set; }

    /// <summary>In the order the customer attached them, however they were read back.</summary>
    public IReadOnlyList<Attachment> Attachments => [.. attachments.OrderBy(attachment => attachment.Position)];

    public static Result<Ticket, DomainError> Raise(
        TicketId id,
        string subject,
        string description,
        TicketPriority priority,
        IReadOnlyList<AttachmentFile> attachments,
        DateTimeOffset raisedAt)
    {
        Ensure.That(id).IsNotNull();
        Ensure.That(subject).IsNotNull();
        Ensure.That(description).IsNotNull();
        Ensure.That(attachments).IsNotNull();

        subject = subject.Trim();
        if (subject.Length == 0 || subject.Length > MaximumSubjectLength)
        {
            return Failure(TicketFailures.InvalidSubject(MaximumSubjectLength));
        }

        description = description.Trim();
        if (description.Length == 0 || description.Length > MaximumDescriptionLength)
        {
            return Failure(TicketFailures.InvalidDescription(MaximumDescriptionLength));
        }

        if (attachments.Count > MaximumAttachments)
        {
            return Failure(TicketFailures.TooManyAttachments(MaximumAttachments));
        }

        var ticket = new Ticket(id, subject, description, priority, raisedAt);
        foreach (var (position, file) in attachments.Index())
        {
            var attachment = Attachment.From(file, position + 1);
            if (attachment.IsFailure)
            {
                return Failure(attachment.Error);
            }

            ticket.attachments.Add(attachment.Value);
        }

        ticket.Raise(new TicketRaised(id));
        return Success(ticket);
    }
}
