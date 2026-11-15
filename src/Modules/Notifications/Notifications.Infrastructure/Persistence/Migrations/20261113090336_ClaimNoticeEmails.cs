using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FabricPilot.Modules.Notifications.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261113090336_ClaimNoticeEmails : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "email_claimed_at",
            schema: "notifications",
            table: "notices",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<uint>(
            name: "xmin",
            schema: "notifications",
            table: "notices",
            type: "xid",
            rowVersion: true,
            nullable: false,
            defaultValue: 0u);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "email_claimed_at",
            schema: "notifications",
            table: "notices");

        migrationBuilder.DropColumn(
            name: "xmin",
            schema: "notifications",
            table: "notices");
    }
}
