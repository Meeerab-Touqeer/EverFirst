using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventSystem.Migrations
{
    /// <inheritdoc />
    public partial class FixProductVendorSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 1,
                column: "VendorId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$IovSW1I73ZqiYwEQ3zgzJ.sjV6xKpNyxuDOPenCuvc6SUDQ5463gC");

            migrationBuilder.InsertData(
                table: "Vendors",
                columns: new[] { "VendorId", "ContactEmail", "CreatedAt", "Name", "Phone", "Status" },
                values: new object[] { 1, "vendor@inventsystem.com", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Default Vendor", "+111111111", "Approved" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Vendors",
                keyColumn: "VendorId",
                keyValue: 1);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 1,
                column: "VendorId",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$KhosVxFGEO3ovny5lbHtdOWwO1FrcGNnRA/ClSvDaSozIKQA2SJAq");
        }
    }
}
