using FabricPilot.BuildingBlocks.Domain;
using FabricPilot.Modules.Tickets.Domain.Tickets.Events;

namespace FabricPilot.Modules.Tickets.Domain.Tickets;

/// <summary>
/// A customer's request for help: what it is about, how urgent it is, and the files that show it; who raised it, and,
/// as staff triage it, where it stands and whom it is assigned to.
/// </summary>
public sealed class Ticket : AggregateRoot<TicketId>
{
    public const int MaximumSubjectLength = 200;

    public const int MaximumDescriptionLength = 4000;

    public const int MaximumAttachments = 5;

    public const int MaximumAssigneeLength = 255;

    private readonly List<Attachment> attachments = [];

    // The customer is an owned type, which EF Core sets after construction; Raise sets it at once.
    private Ticket(TicketId id, string subject, string description, TicketPriority priority, DateTimeOffset raisedAt)
        : base(id)
    {
        Subject = subject;
        Description = description;
        Priority = priority;
        RaisedAt = raisedAt;
        Customer = null!;
    }

    public string Subject { get; private set; }

    public string Description { get; private set; }

    public TicketPriority Priority { get; private set; }

    public DateTimeOffset RaisedAt { get; private set; }

    public TicketCustomer Customer { get; private set; }

    public TicketStatus Status { get; private set; }

    /// <summary>The user name of the staff member the ticket is assigned to; none until it is assigned.</summary>
    public string? Assignee { get; private set; }

    /// <summary>In the order the customer attached them, however they were read back.</summary>
    public IReadOnlyList<Attachment> Attachments => [.. attachments.OrderBy(attachment => attachment.Position)];

    public static Result<Ticket, TicketError> Raise(
        TicketId id,
        string subject,
        string description,
        TicketPriority priority,
        TicketCustomer customer,
        IReadOnlyList<AttachmentFile> attachments,
        DateTimeOffset raisedAt)
    {
        Ensure.That(id).IsNotNull();
        Ensure.That(subject).IsNotNull();
        Ensure.That(description).IsNotNull();
        Ensure.That(customer).IsNotNull();
        Ensure.That(customer.UserId, nameof(customer)).IsNotNullOrWhiteSpace().HasMaxLength(TicketCustomer.MaximumUserIdLength);
        Ensure.That(customer.Email, nameof(customer)).IsNotNullOrWhiteSpace().HasMaxLength(TicketCustomer.MaximumEmailLength);
        Ensure.That(attachments).IsNotNull();

        subject = subject.Trim();
        if (subject.Length == 0 || subject.Length > MaximumSubjectLength)
        {
            return Failure(TicketErrors.InvalidSubject(MaximumSubjectLength));
        }

        description = description.Trim();
        if (description.Length == 0 || description.Length > MaximumDescriptionLength)
        {
            return Failure(TicketErrors.InvalidDescription(MaximumDescriptionLength));
        }

        if (attachments.Count > MaximumAttachments)
        {
            return Failure(TicketErrors.TooManyAttachments(MaximumAttachments));
        }

        var ticket = new Ticket(id, subject, description, priority, raisedAt) { Customer = customer };
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

    /// <summary>Moves the ticket to the status; whether it changed, which it does not when it has that status already.</summary>
    public bool ChangeStatus(TicketStatus status)
    {
        if (status == Status)
        {
            return false;
        }

        var previous = Status;
        Status = status;
        Raise(new TicketStatusChanged(Id, previous, status));
        return true;
    }

    /// <summary>
    /// Assigns the ticket to the staff member with the user name; whether it changed, which it does not when it is
    /// assigned to them already.
    /// </summary>
    public Result<bool, TicketError> AssignTo(string assignee)
    {
        Ensure.That(assignee).IsNotNull();

        assignee = assignee.Trim();
        if (assignee.Length == 0 || assignee.Length > MaximumAssigneeLength)
        {
            return Failure(TicketErrors.InvalidAssignee(MaximumAssigneeLength));
        }

        if (assignee == Assignee)
        {
            return Success(false);
        }

        Assignee = assignee;
        Raise(new TicketAssigned(Id, assignee));
        return Success(true);
    }
}
