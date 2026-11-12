using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FabricPilot.Modules.Knowledge.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261112192701_WriteArticles : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "knowledge");

        migrationBuilder.CreateTable(
            name: "articles",
            schema: "knowledge",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                body = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                state = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_articles", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_articles_state_changed_at",
            schema: "knowledge",
            table: "articles",
            columns: new[] { "state", "changed_at" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "articles",
            schema: "knowledge");
    }
}
