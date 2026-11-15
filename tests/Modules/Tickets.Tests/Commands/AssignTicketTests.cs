using System.Net;
using FabricPilot.Contracts.Identity;
using FabricPilot.Contracts.Tickets;
using FabricPilot.Modules.Tickets.Application;
using FabricPilot.Modules.Tickets.Application.Commands.AssignTicket;
using FabricPilot.Modules.Tickets.Domain.Tickets;

namespace FabricPilot.Modules.Tickets.Tests.Commands;

public class AssignTicketTests
{
    private readonly InMemoryTicketStore tickets = new();
    private readonly RecordingUnitOfWork unitOfWork;
    private readonly IStaffDirectory staff = Substitute.For<IStaffDirectory>();
    private readonly Ticket ticket;

    public AssignTicketTests()
    {
        unitOfWork = new RecordingUnitOfWork(tickets);
        ticket = Ticket.Raise(TicketId.New(), "Printer offline", "Error 42.", TicketPriority.High, Customers.Developer, [], DateTimeOffset.UnixEpoch).Value;
        tickets.Saved.Add(ticket);
        staff.IsStaffAsync("staff", Arg.Any<CancellationToken>()).Returns(true);
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
    public async Task A_staff_members_name_around_spaces_is_checked_without_them()
    {
        var result = await Handle(new AssignTicket(ticket.Id.Value, "  staff "));

        result.IsSuccess.ShouldBeTrue();
        ticket.Assignee.ShouldBe("staff");
    }

    /// <summary>A blank name, a user without the staff role and a name nobody has are all not a staff member.</summary>
    [Theory]
    [InlineData(" ")]
    [InlineData("developer")]
    [InlineData("nobody-at-all")]
    public async Task An_assignee_who_is_not_a_staff_member_is_answered_as_422_and_nothing_changes(string assignee)
    {
        var result = await Handle(new AssignTicket(ticket.Id.Value, assignee));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("tickets.assignee-not-staff");
        result.Error.Status.ShouldBe(HttpStatusCode.UnprocessableEntity);
        ticket.Assignee.ShouldBeNull();
        unitOfWork.Sent.ShouldBeEmpty();
        unitOfWork.Commits.ShouldBe(0);
    }

    [Fact]
    public async Task A_ticket_that_is_not_there_is_not_found_before_anyone_asks_who_the_staff_are()
    {
        var result = await Handle(new AssignTicket(Guid.CreateVersion7(), "staff"));

        result.Error.Code.ShouldBe("tickets.ticket-not-found");
        await staff.DidNotReceiveWithAnyArgs().IsStaffAsync(default!, default);
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
        new AssignTicketHandler(tickets, unitOfWork, staff).HandleAsync(command, TestContext.Current.CancellationToken);
}
