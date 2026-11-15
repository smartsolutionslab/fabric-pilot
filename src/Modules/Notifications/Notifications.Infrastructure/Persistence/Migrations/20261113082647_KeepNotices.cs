using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FabricPilot.Modules.Notifications.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261113082647_KeepNotices : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "notifications");

        migrationBuilder.CreateTable(
            name: "notices",
            schema: "notifications",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                causing_event = table.Column<Guid>(type: "uuid", nullable: false),
                recipient = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                ticket_id = table.Column<Guid>(type: "uuid", nullable: false),
                text = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                emailed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_notices", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_notices_causing_event",
            schema: "notifications",
            table: "notices",
            column: "causing_event",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_notices_recipient_created_at",
            schema: "notifications",
            table: "notices",
            columns: new[] { "recipient", "created_at" });

        migrationBuilder.CreateIndex(
            name: "IX_notices_TenantId",
            schema: "notifications",
            table: "notices",
            column: "TenantId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "notices",
            schema: "notifications");
    }
}
