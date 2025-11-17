using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace pto.track.data.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Events",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Start = table.Column<DateTime>(type: "TEXT", nullable: false),
                End = table.Column<DateTime>(type: "TEXT", nullable: false),
                Text = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                Color = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                ResourceId = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Events", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Resources",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Resources", x => x.Id);
            });

        migrationBuilder.InsertData(
            table: "Resources",
            columns: new[] { "Id", "Name" },
            values: new object[,]
            {
                { 1, "Resource A" },
                { 2, "Resource B" },
                { 3, "Resource C" },
                { 4, "Resource D" },
                { 5, "Resource E" },
                { 6, "Resource F" },
                { 7, "Resource G" },
                { 8, "Resource H" },
                { 9, "Resource I" },
                { 10, "Resource J" }
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Events");

        migrationBuilder.DropTable(
            name: "Resources");
    }
}
