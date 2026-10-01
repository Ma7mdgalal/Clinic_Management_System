using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clinic.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class CMS003_CreateDomainEntities : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Patients",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                Gender = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                ContactNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Patients", x => x.Id);
                table.ForeignKey(
                    name: "FK_Patients_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Specialties",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Specialties", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Doctors",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                SpecialtyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                LicenseNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                ConsultationFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Doctors", x => x.Id);
                table.ForeignKey(
                    name: "FK_Doctors_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Doctors_Specialties_SpecialtyId",
                    column: x => x.SpecialtyId,
                    principalTable: "Specialties",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Doctors_LicenseNumber",
            table: "Doctors",
            column: "LicenseNumber",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Doctors_SpecialtyId",
            table: "Doctors",
            column: "SpecialtyId");

        migrationBuilder.CreateIndex(
            name: "IX_Doctors_UserId",
            table: "Doctors",
            column: "UserId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Patients_UserId",
            table: "Patients",
            column: "UserId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Specialties_Name",
            table: "Specialties",
            column: "Name",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Doctors");

        migrationBuilder.DropTable(
            name: "Patients");

        migrationBuilder.DropTable(
            name: "Specialties");
    }
}
