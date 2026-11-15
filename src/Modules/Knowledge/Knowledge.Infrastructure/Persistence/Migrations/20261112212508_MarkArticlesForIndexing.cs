using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FabricPilot.Modules.Knowledge.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261112212508_MarkArticlesForIndexing : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "needs_indexing",
            schema: "knowledge",
            table: "articles",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "needs_indexing",
            schema: "knowledge",
            table: "articles");
    }
}
