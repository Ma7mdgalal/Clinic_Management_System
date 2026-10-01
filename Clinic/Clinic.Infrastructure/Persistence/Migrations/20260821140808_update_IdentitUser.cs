using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clinic.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class update_IdentitUser : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Doctors_AppUser_UserId",
            table: "Doctors");

        migrationBuilder.DropForeignKey(
            name: "FK_Patients_AppUser_UserId",
            table: "Patients");

        migrationBuilder.DropTable(
            name: "AppUser");

        migrationBuilder.AddColumn<string>(
            name: "Discriminator",
            table: "AspNetUsers",
            type: "nvarchar(13)",
            maxLength: 13,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddForeignKey(
            name: "FK_Doctors_AspNetUsers_UserId",
            table: "Doctors",
            column: "UserId",
            principalTable: "AspNetUsers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Patients_AspNetUsers_UserId",
            table: "Patients",
            column: "UserId",
            principalTable: "AspNetUsers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Doctors_AspNetUsers_UserId",
            table: "Doctors");

        migrationBuilder.DropForeignKey(
            name: "FK_Patients_AspNetUsers_UserId",
            table: "Patients");

        migrationBuilder.DropColumn(
            name: "Discriminator",
            table: "AspNetUsers");

        migrationBuilder.CreateTable(
            name: "AppUser",
            columns: table => new
            {
                Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                AccessFailedCount = table.Column<int>(type: "int", nullable: false),
                ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                NormalizedEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                NormalizedUserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                UserName = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AppUser", x => x.Id);
            });

        migrationBuilder.AddForeignKey(
            name: "FK_Doctors_AppUser_UserId",
            table: "Doctors",
            column: "UserId",
            principalTable: "AppUser",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Patients_AppUser_UserId",
            table: "Patients",
            column: "UserId",
            principalTable: "AppUser",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }
}
