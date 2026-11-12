using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FabricPilot.Modules.Contracts.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261112131932_ListContracts : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "contracts");

        migrationBuilder.CreateTable(
            name: "contracts",
            schema: "contracts",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                product = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                starts_on = table.Column<DateOnly>(type: "date", nullable: false),
                ends_on = table.Column<DateOnly>(type: "date", nullable: false),
                renews_on = table.Column<DateOnly>(type: "date", nullable: true),
                TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_contracts", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_contracts_ends_on",
            schema: "contracts",
            table: "contracts",
            column: "ends_on");

        migrationBuilder.CreateIndex(
            name: "IX_contracts_TenantId",
            schema: "contracts",
            table: "contracts",
            column: "TenantId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "contracts",
            schema: "contracts");
    }
}
