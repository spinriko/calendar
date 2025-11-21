using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pto.track.data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSeedDataWithEmployeeNumbers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ActiveDirectoryId", "Email", "EmployeeNumber", "Name" },
                values: new object[] { "mock-ad-guid-employee", "employee@example.com", "EMP001", "Test Employee" });

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ActiveDirectoryId", "Email", "EmployeeNumber", "Name" },
                values: new object[] { "mock-ad-guid-employee2", "employee2@example.com", "EMP002", "Test Employee 2" });

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ActiveDirectoryId", "Email", "EmployeeNumber", "IsApprover", "Name", "Role" },
                values: new object[] { "mock-ad-guid-manager", "manager@example.com", "MGR001", true, "Test Manager", "Manager" });

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "ActiveDirectoryId", "Email", "EmployeeNumber", "IsApprover", "Name", "Role" },
                values: new object[] { "mock-ad-guid-approver", "approver@example.com", "APR001", true, "Test Approver", "Approver" });

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "ActiveDirectoryId", "Email", "EmployeeNumber", "IsApprover", "Name", "Role" },
                values: new object[] { "mock-ad-guid-admin", "admin@example.com", "ADMIN001", true, "Admin User", "Admin" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ActiveDirectoryId", "Email", "EmployeeNumber", "Name" },
                values: new object[] { null, null, null, "Resource A" });

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ActiveDirectoryId", "Email", "EmployeeNumber", "Name" },
                values: new object[] { null, null, null, "Resource B" });

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ActiveDirectoryId", "Email", "EmployeeNumber", "IsApprover", "Name", "Role" },
                values: new object[] { null, null, null, false, "Resource C", "Employee" });

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "ActiveDirectoryId", "Email", "EmployeeNumber", "IsApprover", "Name", "Role" },
                values: new object[] { null, null, null, false, "Resource D", "Employee" });

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "ActiveDirectoryId", "Email", "EmployeeNumber", "IsApprover", "Name", "Role" },
                values: new object[] { null, null, null, false, "Resource E", "Employee" });
        }
    }
}
