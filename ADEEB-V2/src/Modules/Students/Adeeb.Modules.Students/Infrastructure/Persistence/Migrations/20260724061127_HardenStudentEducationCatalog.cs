using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adeeb.Modules.Students.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenStudentEducationCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "completed_at_utc",
                schema: "students",
                table: "student_education_profiles",
                newName: "profile_completed_at_utc");

            migrationBuilder.RenameIndex(
                name: "UX_regions_root_type_name",
                schema: "students",
                table: "regions",
                newName: "UX_regions_root_type_name_ru");

            migrationBuilder.RenameIndex(
                name: "UX_regions_parent_type_name",
                schema: "students",
                table: "regions",
                newName: "UX_regions_parent_type_name_ru");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "graduated_at_utc",
                schema: "students",
                table: "student_education_profiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_regions_parent_type_name_tg",
                schema: "students",
                table: "regions",
                columns: new[] { "parent_id", "type", "normalized_name_tg" },
                unique: true,
                filter: "parent_id IS NOT NULL AND is_active = true");

            migrationBuilder.CreateIndex(
                name: "UX_regions_root_type_name_tg",
                schema: "students",
                table: "regions",
                columns: new[] { "type", "normalized_name_tg" },
                unique: true,
                filter: "parent_id IS NULL AND is_active = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_regions_parent_type_name_tg",
                schema: "students",
                table: "regions");

            migrationBuilder.DropIndex(
                name: "UX_regions_root_type_name_tg",
                schema: "students",
                table: "regions");

            migrationBuilder.DropColumn(
                name: "graduated_at_utc",
                schema: "students",
                table: "student_education_profiles");

            migrationBuilder.RenameColumn(
                name: "profile_completed_at_utc",
                schema: "students",
                table: "student_education_profiles",
                newName: "completed_at_utc");

            migrationBuilder.RenameIndex(
                name: "UX_regions_root_type_name_ru",
                schema: "students",
                table: "regions",
                newName: "UX_regions_root_type_name");

            migrationBuilder.RenameIndex(
                name: "UX_regions_parent_type_name_ru",
                schema: "students",
                table: "regions",
                newName: "UX_regions_parent_type_name");
        }
    }
}
