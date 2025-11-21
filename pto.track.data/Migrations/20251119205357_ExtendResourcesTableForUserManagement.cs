using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pto.track.data.Migrations
{
    /// <inheritdoc />
    public partial class ExtendResourcesTableForUserManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActiveDirectoryId",
                table: "Resources",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Resources",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "Resources",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Resources",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployeeNumber",
                table: "Resources",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Resources",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsApprover",
                table: "Resources",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncDate",
                table: "Resources",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ManagerId",
                table: "Resources",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "Resources",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Resources",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ActiveDirectoryId", "CreatedDate", "Department", "Email", "EmployeeNumber", "IsActive", "IsApprover", "LastSyncDate", "ManagerId", "ModifiedDate", "Role" },
                values: new object[] { null, new DateTime(2025, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, true, false, null, null, new DateTime(2025, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc), "Employee" });

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ActiveDirectoryId", "CreatedDate", "Department", "Email", "EmployeeNumber", "IsActive", "IsApprover", "LastSyncDate", "ManagerId", "ModifiedDate", "Role" },
                values: new object[] { null, new DateTime(2025, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, true, false, null, null, new DateTime(2025, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc), "Employee" });

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ActiveDirectoryId", "CreatedDate", "Department", "Email", "EmployeeNumber", "IsActive", "IsApprover", "LastSyncDate", "ManagerId", "ModifiedDate", "Role" },
                values: new object[] { null, new DateTime(2025, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, true, false, null, null, new DateTime(2025, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc), "Employee" });

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "ActiveDirectoryId", "CreatedDate", "Department", "Email", "EmployeeNumber", "IsActive", "IsApprover", "LastSyncDate", "ManagerId", "ModifiedDate", "Role" },
                values: new object[] { null, new DateTime(2025, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, true, false, null, null, new DateTime(2025, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc), "Employee" });

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "ActiveDirectoryId", "CreatedDate", "Department", "Email", "EmployeeNumber", "IsActive", "IsApprover", "LastSyncDate", "ManagerId", "ModifiedDate", "Role" },
                values: new object[] { null, new DateTime(2025, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, true, false, null, null, new DateTime(2025, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc), "Employee" });

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "ActiveDirectoryId", "CreatedDate", "Department", "Email", "EmployeeNumber", "IsActive", "IsApprover", "LastSyncDate", "ManagerId", "ModifiedDate", "Role" },
                values: new object[] { null, new DateTime(2025, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, true, false, null, null, new DateTime(2025, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc), "Employee" });

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "ActiveDirectoryId", "CreatedDate", "Department", "Email", "EmployeeNumber", "IsActive", "IsApprover", "LastSyncDate", "ManagerId", "ModifiedDate", "Role" },
                values: new object[] { null, new DateTime(2025, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, true, false, null, null, new DateTime(2025, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc), "Employee" });

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "ActiveDirectoryId", "CreatedDate", "Department", "Email", "EmployeeNumber", "IsActive", "IsApprover", "LastSyncDate", "ManagerId", "ModifiedDate", "Role" },
                values: new object[] { null, new DateTime(2025, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, true, false, null, null, new DateTime(2025, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc), "Employee" });

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "ActiveDirectoryId", "CreatedDate", "Department", "Email", "EmployeeNumber", "IsActive", "IsApprover", "LastSyncDate", "ManagerId", "ModifiedDate", "Role" },
                values: new object[] { null, new DateTime(2025, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, true, false, null, null, new DateTime(2025, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc), "Employee" });

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "ActiveDirectoryId", "CreatedDate", "Department", "Email", "EmployeeNumber", "IsActive", "IsApprover", "LastSyncDate", "ManagerId", "ModifiedDate", "Role" },
                values: new object[] { null, new DateTime(2025, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, true, false, null, null, new DateTime(2025, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc), "Employee" });

            migrationBuilder.CreateIndex(
                name: "IX_Resources_ManagerId",
                table: "Resources",
                column: "ManagerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Resources_Resources_ManagerId",
                table: "Resources",
                column: "ManagerId",
                principalTable: "Resources",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Resources_Resources_ManagerId",
                table: "Resources");

            migrationBuilder.DropIndex(
                name: "IX_Resources_ManagerId",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "ActiveDirectoryId",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "EmployeeNumber",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "IsApprover",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "LastSyncDate",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "ManagerId",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Resources");
        }
    }
}
