using System.Net;
using FabricPilot.Modules.Tickets.Application.Queries.GetStaffTicket;
using FabricPilot.Modules.Tickets.Domain.Tickets;

namespace FabricPilot.Modules.Tickets.Tests.Queries;

public class GetStaffTicketTests
{
    private static readonly DateTimeOffset RaisedAt = new(2026, 11, 3, 14, 0, 0, TimeSpan.Zero);

    private readonly InMemoryTicketStore store = new();

    [Fact]
    public async Task Staff_read_a_ticket_with_its_tenant_status_assignee_and_attachments()
    {
        var attachment = new AttachmentFile(AttachmentId.New(), "invoice.pdf", "application/pdf", 1234);
        var ticket = Ticket.Raise(TicketId.New(), "Wrong invoice", "The total is off.", TicketPriority.Normal, Customers.Developer, [attachment], RaisedAt).Value;
        ticket.ChangeStatus(TicketStatus.InProgress);
        ticket.AssignTo("staff");
        store.Saved.Add(ticket);
        store.Tenants[ticket.Id] = "tenant-b";

        var result = await new GetStaffTicketHandler(store).HandleAsync(new GetStaffTicket(ticket.Id.Value), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var dto = result.Value;
        dto.Id.ShouldBe(ticket.Id.Value);
        dto.Tenant.ShouldBe("tenant-b");
        dto.Subject.ShouldBe("Wrong invoice");
        dto.Description.ShouldBe("The total is off.");
        dto.Priority.ShouldBe("Normal");
        dto.Status.ShouldBe("InProgress");
        dto.Assignee.ShouldBe("staff");
        dto.RaisedAt.ShouldBe(RaisedAt);
        dto.Attachments.ShouldHaveSingleItem().FileName.ShouldBe("invoice.pdf");
    }

    [Fact]
    public async Task A_ticket_that_is_not_there_is_not_found()
    {
        var result = await new GetStaffTicketHandler(store).HandleAsync(new GetStaffTicket(Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("tickets.ticket-not-found");
        result.Error.Status.ShouldBe(HttpStatusCode.NotFound);
    }
}
