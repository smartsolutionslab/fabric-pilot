using FabricPilot.Modules.Tickets.Application.DTOs;
using FabricPilot.Modules.Tickets.Domain.Tickets;
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
}
