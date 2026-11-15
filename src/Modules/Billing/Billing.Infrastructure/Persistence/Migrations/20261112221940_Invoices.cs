using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FabricPilot.Modules.Billing.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261112221940_Invoices : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "billing");

        migrationBuilder.CreateTable(
            name: "invoices",
            schema: "billing",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                issued_on = table.Column<DateOnly>(type: "date", nullable: false),
                due_on = table.Column<DateOnly>(type: "date", nullable: false),
                currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_invoices", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "invoice_lines",
            schema: "billing",
            columns: table => new
            {
                position = table.Column<int>(type: "integer", nullable: false),
                invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                quantity = table.Column<int>(type: "integer", nullable: false),
                unit_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_invoice_lines", x => new { x.invoice_id, x.position });
                table.ForeignKey(
                    name: "FK_invoice_lines_invoices_invoice_id",
                    column: x => x.invoice_id,
                    principalSchema: "billing",
                    principalTable: "invoices",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_invoices_issued_on",
            schema: "billing",
            table: "invoices",
            column: "issued_on");

        migrationBuilder.CreateIndex(
            name: "IX_invoices_TenantId",
            schema: "billing",
            table: "invoices",
            column: "TenantId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "invoice_lines",
            schema: "billing");

        migrationBuilder.DropTable(
            name: "invoices",
            schema: "billing");
    }
}
