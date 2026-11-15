using FabricPilot.Modules.Tickets.Application;
using FabricPilot.Modules.Tickets.Application.DTOs;
using FabricPilot.Modules.Tickets.Application.Queries.ListTicketQueue;
using FabricPilot.Modules.Tickets.Domain.Tickets;
using FabricPilot.Modules.Tickets.Infrastructure;
using FabricPilot.Modules.Tickets.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FabricPilot.Modules.Tickets.Tests.DTOs;

public class TicketMappingsTests
{
    private static readonly DateTimeOffset RaisedAt = new(2026, 11, 12, 9, 30, 0, TimeSpan.Zero);

    [Fact]
    public void A_ticket_is_summarised_with_its_subject_priority_time_and_attachment_count()
    {
        var ticket = Ticket.Raise(
            TicketId.New(),
            "Printer offline",
            "It shows error 42.",
            TicketPriority.High,
            Customers.Developer,
            [new AttachmentFile(AttachmentId.New(), "a.png", "image/png", 1), new AttachmentFile(AttachmentId.New(), "b.log", "text/plain", 1)],
            RaisedAt).Value;

        var summary = TicketMappings.ToTicketSummaryDtoExpression().Compile()(ticket);

        summary.ShouldBe(new TicketSummaryDto(ticket.Id.Value, "Printer offline", "High", RaisedAt, 2));
    }

    [Fact]
    public void The_summary_is_projected_in_the_database_counting_attachments_without_reading_them_or_the_description()
    {
        using var context = new TicketsDbContextFactory().CreateDbContext([]);

        var sql = context.Set<Ticket>().Select(TicketMappings.ToTicketSummaryDtoExpression()).ToQueryString();

        sql.ShouldContain("count(*)", Case.Insensitive);
        sql.ShouldNotContain("description");
        sql.ShouldNotContain("file_name");
        sql.ShouldNotContain("content_type");
    }

    [Fact]
    public void A_queued_ticket_shows_its_tenant_status_and_assignee_beside_the_summary()
    {
        var ticket = Ticket.Raise(TicketId.New(), "Printer offline", "It shows error 42.", TicketPriority.Urgent, Customers.Developer, [], RaisedAt).Value;
        ticket.ChangeStatus(TicketStatus.InProgress);
        ticket.AssignTo("staff");

        var queued = TicketMappings.ToQueuedTicketDtoExpression(_ => "tenant-b").Compile()(ticket);

        queued.ShouldBe(new QueuedTicketDto(ticket.Id.Value, "tenant-b", "Printer offline", "Urgent", "InProgress", "staff", RaisedAt, 0));
    }

    [Fact]
    public void A_staff_ticket_shows_its_tenant_status_and_assignee_beside_the_ticket()
    {
        var ticket = Ticket.Raise(TicketId.New(), "Printer offline", "It shows error 42.", TicketPriority.Urgent, Customers.Developer, [], RaisedAt).Value;

        var dto = new TenantTicket("tenant-a", ticket).ToStaffTicketDto();

        (dto.Id, dto.Tenant, dto.Subject, dto.Description, dto.Priority, dto.Status, dto.Assignee, dto.RaisedAt)
            .ShouldBe((ticket.Id.Value, "tenant-a", "Printer offline", "It shows error 42.", "Urgent", "Open", (string?)null, RaisedAt));
        dto.Attachments.ShouldBeEmpty();
    }

    [Fact]
    public void The_queue_is_filtered_and_projected_in_the_database_reading_each_rows_tenant_and_not_the_description()
    {
        using var context = new TicketsDbContextFactory().CreateDbContext([]);
        var filter = new TicketQueueFilter(
            Option<string>.Some("tenant-b"),
            Option<TicketStatus>.Some(TicketStatus.Open),
            Option<TicketPriority>.Some(TicketPriority.High));

        var sql = context.Set<Ticket>()
            .Matching(filter, TicketStore.TenantOf)
            .Select(TicketMappings.ToQueuedTicketDtoExpression(TicketStore.TenantOf))
            .ToQueryString();

        sql.ShouldContain("\"TenantId\" = ");
        sql.ShouldContain("status");
        sql.ShouldContain("priority");
        sql.ShouldContain("assignee");
        sql.ShouldContain("count(*)", Case.Insensitive);
        sql.ShouldNotContain("description");
        sql.ShouldNotContain("file_name");
    }
}
