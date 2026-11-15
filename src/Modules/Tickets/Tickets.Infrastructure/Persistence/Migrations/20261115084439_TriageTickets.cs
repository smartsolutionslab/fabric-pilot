using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FabricPilot.Modules.Tickets.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261115084439_TriageTickets : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "assignee",
            schema: "tickets",
            table: "tickets",
            type: "character varying(255)",
            maxLength: 255,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "customer_email",
            schema: "tickets",
            table: "tickets",
            type: "character varying(320)",
            maxLength: 320,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "customer_user_id",
            schema: "tickets",
            table: "tickets",
            type: "character varying(64)",
            maxLength: 64,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "status",
            schema: "tickets",
            table: "tickets",
            type: "character varying(16)",
            maxLength: 16,
            nullable: false,
            defaultValue: "Open");

        migrationBuilder.CreateIndex(
            name: "IX_tickets_status",
            schema: "tickets",
            table: "tickets",
            column: "status");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_tickets_status",
            schema: "tickets",
            table: "tickets");

        migrationBuilder.DropColumn(
            name: "assignee",
            schema: "tickets",
            table: "tickets");

        migrationBuilder.DropColumn(
            name: "customer_email",
            schema: "tickets",
            table: "tickets");

        migrationBuilder.DropColumn(
            name: "customer_user_id",
            schema: "tickets",
            table: "tickets");

        migrationBuilder.DropColumn(
            name: "status",
            schema: "tickets",
            table: "tickets");
    }
}
