using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FabricPilot.Modules.Tickets.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261112060650_RaiseTickets : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "tickets");

        migrationBuilder.CreateTable(
            name: "tickets",
            schema: "tickets",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                priority = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                raised_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_tickets", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "ticket_attachments",
            schema: "tickets",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                position = table.Column<int>(type: "integer", nullable: false),
                file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                content_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                size = table.Column<long>(type: "bigint", nullable: false),
                ticket_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ticket_attachments", x => x.id);
                table.ForeignKey(
                    name: "FK_ticket_attachments_tickets_ticket_id",
                    column: x => x.ticket_id,
                    principalSchema: "tickets",
                    principalTable: "tickets",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ticket_attachments_ticket_id",
            schema: "tickets",
            table: "ticket_attachments",
            column: "ticket_id");

        migrationBuilder.CreateIndex(
            name: "IX_tickets_raised_at",
            schema: "tickets",
            table: "tickets",
            column: "raised_at");

        migrationBuilder.CreateIndex(
            name: "IX_tickets_TenantId",
            schema: "tickets",
            table: "tickets",
            column: "TenantId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ticket_attachments",
            schema: "tickets");

        migrationBuilder.DropTable(
            name: "tickets",
            schema: "tickets");
    }
}
