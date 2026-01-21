using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pto.track.data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdpFieldsToResource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssociateId",
                table: "Resources",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DepartmentCode",
                table: "Resources",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JobCode",
                table: "Resources",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JobTitle",
                table: "Resources",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagerAssociateId",
                table: "Resources",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AssociateId", "DepartmentCode", "JobCode", "JobTitle", "ManagerAssociateId" },
                values: new object[] { null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "AssociateId", "DepartmentCode", "JobCode", "JobTitle", "ManagerAssociateId" },
                values: new object[] { null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "AssociateId", "DepartmentCode", "JobCode", "JobTitle", "ManagerAssociateId" },
                values: new object[] { null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "AssociateId", "DepartmentCode", "JobCode", "JobTitle", "ManagerAssociateId" },
                values: new object[] { null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "AssociateId", "DepartmentCode", "JobCode", "JobTitle", "ManagerAssociateId" },
                values: new object[] { null, null, null, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_Resources_AssociateId",
                table: "Resources",
                column: "AssociateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Resources_AssociateId",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "AssociateId",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "DepartmentCode",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "JobCode",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "JobTitle",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "ManagerAssociateId",
                table: "Resources");
        }
    }
}
