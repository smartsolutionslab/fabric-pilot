using System.Net;
using FabricPilot.Modules.Tickets.Application.Queries.GetTicket;
using FabricPilot.Modules.Tickets.Domain.Tickets;

namespace FabricPilot.Modules.Tickets.Tests.Queries;

public class GetTicketTests
{
    private static readonly DateTimeOffset RaisedAt = new(2026, 11, 3, 14, 0, 0, TimeSpan.Zero);

    private readonly InMemoryTicketStore store = new();

    [Fact]
    public async Task A_ticket_is_shown_with_its_attachments()
    {
        var attachment = new AttachmentFile(AttachmentId.New(), "invoice.pdf", "application/pdf", 1234);
        var ticket = Ticket.Raise(TicketId.New(), "Wrong invoice", "The total is off.", TicketPriority.Normal, [attachment], RaisedAt).Value;
        store.Saved.Add(ticket);

        var result = await new GetTicketHandler(store).HandleAsync(new GetTicket(ticket.Id.Value), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var dto = result.Value;
        dto.Id.ShouldBe(ticket.Id.Value);
        dto.Subject.ShouldBe("Wrong invoice");
        dto.Description.ShouldBe("The total is off.");
        dto.Priority.ShouldBe("Normal");
        dto.RaisedAt.ShouldBe(RaisedAt);
        var shown = dto.Attachments.ShouldHaveSingleItem();
        shown.Id.ShouldBe(attachment.Id.Value);
        shown.FileName.ShouldBe("invoice.pdf");
        shown.ContentType.ShouldBe("application/pdf");
        shown.Size.ShouldBe(1234);
    }

    [Fact]
    public async Task A_ticket_that_is_not_there_is_not_found()
    {
        var result = await new GetTicketHandler(store).HandleAsync(new GetTicket(Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("tickets.ticket-not-found");
        result.Error.Status.ShouldBe(HttpStatusCode.NotFound);
    }
}
