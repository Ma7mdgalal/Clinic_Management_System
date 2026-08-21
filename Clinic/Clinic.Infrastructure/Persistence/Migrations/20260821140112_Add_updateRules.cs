using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Clinic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_updateRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationRole");

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "AspNetRoles",
                type: "nvarchar(21)",
                maxLength: 21,
                nullable: false,
                defaultValue: "");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Discriminator", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "1a5e1a1a-0000-0000-0000-000000000001", "1a5e1a1a-0000-0000-0000-000000001001", "ApplicationRole", "Admin", "ADMIN" },
                    { "1a5e1a1a-0000-0000-0000-000000000002", "1a5e1a1a-0000-0000-0000-000000001002", "ApplicationRole", "Doctor", "DOCTOR" },
                    { "1a5e1a1a-0000-0000-0000-000000000003", "1a5e1a1a-0000-0000-0000-000000001003", "ApplicationRole", "Patient", "PATIENT" },
                    { "1a5e1a1a-0000-0000-0000-000000000004", "1a5e1a1a-0000-0000-0000-000000001004", "ApplicationRole", "Receptionist", "RECEPTIONIST" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "1a5e1a1a-0000-0000-0000-000000000001");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "1a5e1a1a-0000-0000-0000-000000000002");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "1a5e1a1a-0000-0000-0000-000000000003");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "1a5e1a1a-0000-0000-0000-000000000004");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "AspNetRoles");

            migrationBuilder.CreateTable(
                name: "ApplicationRole",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationRole", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ApplicationRole",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "1a5e1a1a-0000-0000-0000-000000000001", "1a5e1a1a-0000-0000-0000-000000001001", "Admin", "ADMIN" },
                    { "1a5e1a1a-0000-0000-0000-000000000002", "1a5e1a1a-0000-0000-0000-000000001002", "Doctor", "DOCTOR" },
                    { "1a5e1a1a-0000-0000-0000-000000000003", "1a5e1a1a-0000-0000-0000-000000001003", "Patient", "PATIENT" },
                    { "1a5e1a1a-0000-0000-0000-000000000004", "1a5e1a1a-0000-0000-0000-000000001004", "Receptionist", "RECEPTIONIST" }
                });
        }
    }
}
