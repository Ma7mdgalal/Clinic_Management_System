using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Clinic.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class CMS010_AlignDomainWithReadme_RemoveIdentityTph : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Patients_UserId",
            table: "Patients");

        migrationBuilder.DropIndex(
            name: "IX_Doctors_SpecialtyId",
            table: "Doctors");

        // ContactNumber -> Phone is a RENAME, and EF cannot detect renames: it compares
        // the old and new models column by column, sees one name gone and another
        // appeared, and scaffolds DropColumn + AddColumn. That is not equivalent — it
        // throws away every stored phone number. Renaming is the corrected version, and
        // hand-editing scaffolded migrations for exactly this case is normal practice.
        migrationBuilder.RenameColumn(
            name: "ContactNumber",
            table: "Patients",
            newName: "Phone");

        // The old column was nullable; the new one is required. Any existing NULL would
        // make the ALTER below fail, so they are filled first. The table is empty today,
        // which makes this a no-op — but a migration has to be correct against the
        // database it will actually meet in production, not the one on this machine.
        migrationBuilder.Sql("UPDATE [Patients] SET [Phone] = '' WHERE [Phone] IS NULL;");

        migrationBuilder.AlterColumn<string>(
            name: "Phone",
            table: "Patients",
            type: "nvarchar(30)",
            maxLength: 30,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(15)",
            oldMaxLength: 15,
            oldNullable: true);

        // Dropping the accidental TPH discriminator. Nothing needs to be migrated into
        // it: every existing row already holds 'AppUser' / 'ApplicationRole', which was
        // the only value the model could ever produce.
        migrationBuilder.DropColumn(
            name: "Discriminator",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "Discriminator",
            table: "AspNetRoles");

        migrationBuilder.AlterColumn<string>(
            name: "UserId",
            table: "Patients",
            type: "nvarchar(450)",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(450)");

        migrationBuilder.AlterColumn<string>(
            name: "Gender",
            table: "Patients",
            type: "nvarchar(20)",
            maxLength: 20,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(10)",
            oldMaxLength: 10,
            oldNullable: true);

        migrationBuilder.AlterColumn<DateOnly>(
            name: "DateOfBirth",
            table: "Patients",
            type: "date",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "datetime2");

        migrationBuilder.AddColumn<string>(
            name: "Email",
            table: "Patients",
            type: "nvarchar(256)",
            maxLength: 256,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "EmergencyContact",
            table: "Patients",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Email",
            table: "Doctors",
            type: "nvarchar(256)",
            maxLength: 256,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "Phone",
            table: "Doctors",
            type: "nvarchar(30)",
            maxLength: 30,
            nullable: false,
            defaultValue: "");

        migrationBuilder.CreateIndex(
            name: "IX_Patients_LastName_FirstName",
            table: "Patients",
            columns: new[] { "LastName", "FirstName" });

        migrationBuilder.CreateIndex(
            name: "IX_Patients_Phone",
            table: "Patients",
            column: "Phone");

        migrationBuilder.CreateIndex(
            name: "IX_Patients_UserId",
            table: "Patients",
            column: "UserId",
            unique: true,
            filter: "[UserId] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_Doctors_SpecialtyId_IsActive",
            table: "Doctors",
            columns: new[] { "SpecialtyId", "IsActive" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Patients_LastName_FirstName",
            table: "Patients");

        migrationBuilder.DropIndex(
            name: "IX_Patients_Phone",
            table: "Patients");

        migrationBuilder.DropIndex(
            name: "IX_Patients_UserId",
            table: "Patients");

        migrationBuilder.DropIndex(
            name: "IX_Doctors_SpecialtyId_IsActive",
            table: "Doctors");

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
            name: "Email",
            table: "Patients");

        migrationBuilder.DropColumn(
            name: "EmergencyContact",
            table: "Patients");

        migrationBuilder.DropColumn(
            name: "Email",
            table: "Doctors");

        migrationBuilder.DropColumn(
            name: "Phone",
            table: "Doctors");

        migrationBuilder.AlterColumn<string>(
            name: "UserId",
            table: "Patients",
            type: "nvarchar(450)",
            nullable: false,
            defaultValue: "",
            oldClrType: typeof(string),
            oldType: "nvarchar(450)",
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "Gender",
            table: "Patients",
            type: "nvarchar(10)",
            maxLength: 10,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(20)",
            oldMaxLength: 20,
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "DateOfBirth",
            table: "Patients",
            type: "datetime2",
            nullable: false,
            oldClrType: typeof(DateOnly),
            oldType: "date");

        // Mirror image of the rename in Up(): widen back down and restore the old name,
        // so Down() genuinely reverses Up() instead of destroying the column. A Down()
        // that loses data is a Down() nobody dares run.
        migrationBuilder.AlterColumn<string>(
            name: "Phone",
            table: "Patients",
            type: "nvarchar(15)",
            maxLength: 15,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(30)",
            oldMaxLength: 30);

        migrationBuilder.RenameColumn(
            name: "Phone",
            table: "Patients",
            newName: "ContactNumber");

        migrationBuilder.AddColumn<string>(
            name: "Discriminator",
            table: "AspNetUsers",
            type: "nvarchar(13)",
            maxLength: 13,
            nullable: false,
            defaultValue: "");

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

        migrationBuilder.CreateIndex(
            name: "IX_Patients_UserId",
            table: "Patients",
            column: "UserId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Doctors_SpecialtyId",
            table: "Doctors",
            column: "SpecialtyId");
    }
}
