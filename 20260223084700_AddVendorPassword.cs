using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddVendorPassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Password",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$7ifXAJ7WSA1cSHvSs8lSR.6O/IZKkAcJWRViijjEbq.KmKBouta6S");

            migrationBuilder.UpdateData(
                table: "Vendors",
                keyColumn: "VendorId",
                keyValue: 1,
                column: "Password",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Password",
                table: "Vendors");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$ukjFbgMMyymm82vlDyNWoOoIfffg38qVmvWplKP7LmJ7ARKaDETqu");
        }
    }
}
