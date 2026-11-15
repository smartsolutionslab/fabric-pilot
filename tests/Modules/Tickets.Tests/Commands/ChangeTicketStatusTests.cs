using System.Net;
using FabricPilot.Contracts.Tickets;
using FabricPilot.Modules.Tickets.Application;
using FabricPilot.Modules.Tickets.Application.Commands.ChangeTicketStatus;
using FabricPilot.Modules.Tickets.Domain.Tickets;

namespace FabricPilot.Modules.Tickets.Tests.Commands;

public class ChangeTicketStatusTests
{
    private readonly InMemoryTicketStore tickets = new();
    private readonly RecordingUnitOfWork unitOfWork;
    private readonly Ticket ticket;

    public ChangeTicketStatusTests()
    {
        unitOfWork = new RecordingUnitOfWork(tickets);
        ticket = Ticket.Raise(TicketId.New(), "Printer offline", "Error 42.", TicketPriority.High, Customers.Developer, [], DateTimeOffset.UnixEpoch).Value;
        tickets.Saved.Add(ticket);
    }

    [Fact]
    public async Task A_status_change_is_saved_with_the_event_that_tells_the_customer_who_raised_the_ticket()
    {
        var result = await Handle(new ChangeTicketStatus(ticket.Id.Value, "Resolved"));

        result.IsSuccess.ShouldBeTrue();
        ticket.Status.ShouldBe(TicketStatus.Resolved);
        unitOfWork.Commits.ShouldBe(1);
        unitOfWork.Sent.ShouldHaveSingleItem().ShouldBe(new TicketStatusChangedV1(
            ticket.Id.Value,
            "Printer offline",
            "Open",
            "Resolved",
            Customers.Developer.UserId,
            Customers.Developer.Email));
    }

    [Fact]
    public async Task Each_change_tells_the_status_it_replaced()
    {
        await Handle(new ChangeTicketStatus(ticket.Id.Value, "InProgress"));

        await Handle(new ChangeTicketStatus(ticket.Id.Value, "Closed"));

        unitOfWork.Sent.OfType<TicketStatusChangedV1>().Select(changed => (changed.PreviousStatus, changed.Status))
            .ShouldBe([("Open", "InProgress"), ("InProgress", "Closed")]);
    }

    [Fact]
    public async Task Setting_the_status_a_ticket_has_already_succeeds_and_tells_nobody()
    {
        var result = await Handle(new ChangeTicketStatus(ticket.Id.Value, "Open"));

        result.IsSuccess.ShouldBeTrue();
        unitOfWork.Sent.ShouldBeEmpty();
        unitOfWork.Commits.ShouldBe(0);
    }

    [Theory]
    [InlineData("Done")]
    [InlineData("resolved")]
    [InlineData("2")]
    [InlineData("")]
    public async Task A_status_that_is_not_one_of_the_named_ones_is_refused_and_nothing_changes(string status)
    {
        var result = await Handle(new ChangeTicketStatus(ticket.Id.Value, status));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("tickets.unknown-status");
        result.Error.Status.ShouldBe(HttpStatusCode.UnprocessableEntity);
        ticket.Status.ShouldBe(TicketStatus.Open);
        unitOfWork.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_ticket_that_is_not_there_is_not_found()
    {
        var result = await Handle(new ChangeTicketStatus(Guid.CreateVersion7(), "Resolved"));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("tickets.ticket-not-found");
        result.Error.Status.ShouldBe(HttpStatusCode.NotFound);
        unitOfWork.Sent.ShouldBeEmpty();
    }

    private Task<Result<Unit, ApiError>> Handle(ChangeTicketStatus command) =>
        new ChangeTicketStatusHandler(tickets, unitOfWork).HandleAsync(command, TestContext.Current.CancellationToken);
}
