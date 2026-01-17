using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace pto.track.data.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupsAndUpdateResources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.AddColumn<int>(
                name: "GroupId",
                table: "Resources",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    GroupId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.GroupId);
                });

            migrationBuilder.InsertData(
                table: "Groups",
                columns: new[] { "GroupId", "Name" },
                values: new object[] { 1, "Group 1" });

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "GroupId", "Name" },
                values: new object[] { 1, "Test Employee 1" });

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: 2,
                column: "GroupId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "GroupId", "Name" },
                values: new object[] { 1, "Manager" });

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "GroupId", "Name" },
                values: new object[] { 1, "Approver" });

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "GroupId", "Name" },
                values: new object[] { 1, "Administrator" });

            // Ensure all remaining resources are assigned to a valid group before adding FK constraint.
            // If migration runs multiple times, orphaned records with GroupId=0 must be updated.
            migrationBuilder.Sql(
                "UPDATE Resources SET GroupId = 1 WHERE GroupId = 0;");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_GroupId",
                table: "Resources",
                column: "GroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Resources_Groups_GroupId",
                table: "Resources",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "GroupId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Resources_Groups_GroupId",
                table: "Resources");

            migrationBuilder.DropTable(
                name: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Resources_GroupId",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "Resources");

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Test Employee");

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: 3,
                column: "Name",
                value: "Test Manager");

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: 4,
                column: "Name",
                value: "Test Approver");

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: 5,
                column: "Name",
                value: "Admin User");

            migrationBuilder.InsertData(
                table: "Resources",
                columns: new[] { "Id", "ActiveDirectoryId", "CreatedDate", "Department", "Email", "EmployeeNumber", "IsActive", "IsApprover", "LastSyncDate", "ManagerId", "ModifiedDate", "Name", "Role" },
                values: new object[,]
                {
                    { 6, null, new DateTime(2025, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, true, false, null, null, new DateTime(2025, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc), "Resource F", "Employee" },
                    { 7, null, new DateTime(2025, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, true, false, null, null, new DateTime(2025, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc), "Resource G", "Employee" },
                    { 8, null, new DateTime(2025, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, true, false, null, null, new DateTime(2025, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc), "Resource H", "Employee" },
                    { 9, null, new DateTime(2025, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, true, false, null, null, new DateTime(2025, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc), "Resource I", "Employee" },
                    { 10, null, new DateTime(2025, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, true, false, null, null, new DateTime(2025, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc), "Resource J", "Employee" }
                });
        }
    }
}
