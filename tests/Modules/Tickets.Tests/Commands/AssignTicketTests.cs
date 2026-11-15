using System.Net;
using FabricPilot.Contracts.Tickets;
using FabricPilot.Modules.Tickets.Application;
using FabricPilot.Modules.Tickets.Application.Commands.AssignTicket;
using FabricPilot.Modules.Tickets.Domain.Tickets;

namespace FabricPilot.Modules.Tickets.Tests.Commands;

public class AssignTicketTests
{
    private readonly InMemoryTicketStore tickets = new();
    private readonly RecordingUnitOfWork unitOfWork;
    private readonly Ticket ticket;

    public AssignTicketTests()
    {
        unitOfWork = new RecordingUnitOfWork(tickets);
        ticket = Ticket.Raise(TicketId.New(), "Printer offline", "Error 42.", TicketPriority.High, Customers.Developer, [], DateTimeOffset.UnixEpoch).Value;
        tickets.Saved.Add(ticket);
    }

    [Fact]
    public async Task An_assignment_is_saved_with_the_event_that_announces_it()
    {
        var result = await Handle(new AssignTicket(ticket.Id.Value, "staff"));

        result.IsSuccess.ShouldBeTrue();
        ticket.Assignee.ShouldBe("staff");
        unitOfWork.Commits.ShouldBe(1);
        unitOfWork.Sent.ShouldHaveSingleItem().ShouldBe(new TicketAssignedV1(ticket.Id.Value, "Printer offline", "staff"));
    }

    [Fact]
    public async Task Assigning_a_ticket_to_whom_it_is_assigned_already_succeeds_and_announces_nothing()
    {
        await Handle(new AssignTicket(ticket.Id.Value, "staff"));

        var result = await Handle(new AssignTicket(ticket.Id.Value, "staff"));

        result.IsSuccess.ShouldBeTrue();
        unitOfWork.Sent.ShouldHaveSingleItem();
        unitOfWork.Commits.ShouldBe(1);
    }

    [Fact]
    public async Task An_assignee_the_ticket_refuses_is_answered_as_422_and_nothing_changes()
    {
        var result = await Handle(new AssignTicket(ticket.Id.Value, " "));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("tickets.invalid-assignee");
        result.Error.Status.ShouldBe(HttpStatusCode.UnprocessableEntity);
        ticket.Assignee.ShouldBeNull();
        unitOfWork.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_ticket_that_is_not_there_is_not_found()
    {
        var result = await Handle(new AssignTicket(Guid.CreateVersion7(), "staff"));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("tickets.ticket-not-found");
        result.Error.Status.ShouldBe(HttpStatusCode.NotFound);
    }

    private Task<Result<Unit, ApiError>> Handle(AssignTicket command) =>
        new AssignTicketHandler(tickets, unitOfWork).HandleAsync(command, TestContext.Current.CancellationToken);
}
