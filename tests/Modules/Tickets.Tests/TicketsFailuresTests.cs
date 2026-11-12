using System.Net;
using FabricPilot.Modules.Tickets.Application;
using FabricPilot.Modules.Tickets.Domain.Tickets;

namespace FabricPilot.Modules.Tickets.Tests;

public class TicketsFailuresTests
{
    [Fact]
    public void Each_way_raising_a_ticket_fails_is_answered_as_422_with_its_own_code_and_message()
    {
        TicketError[] errors =
        [
            new InvalidSubject(Ticket.MaximumSubjectLength),
            new InvalidDescription(Ticket.MaximumDescriptionLength),
            new TooManyAttachments(Ticket.MaximumAttachments),
            new AttachmentTooLarge("huge.iso", Attachment.MaximumSize),
            new InvalidAttachmentName(Attachment.MaximumFileNameLength),
        ];

        var failures = errors.Select(TicketsFailures.From).ToList();

        failures.Select(failure => failure.Code).ShouldBe(
        [
            "tickets.invalid-subject",
            "tickets.invalid-description",
            "tickets.too-many-attachments",
            "tickets.attachment-too-large",
            "tickets.invalid-attachment-name",
        ]);
        failures.ShouldAllBe(failure => failure.Status == HttpStatusCode.UnprocessableEntity);
        failures.Select(failure => failure.Message).ShouldBe(errors.Select(error => ((DomainError)error.Value!).Message));
    }
}
