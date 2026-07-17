using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adeeb.Modules.Mmt.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogImportProgramDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_mmt_admission_program_identity",
                schema: "mmt",
                table: "admission_programs");

            migrationBuilder.AddColumn<string>(
                name: "normalized_full_name_ru",
                schema: "mmt",
                table: "universities",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "normalized_study_location",
                schema: "mmt",
                table: "admission_programs",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "study_location_ru",
                schema: "mmt",
                table: "admission_programs",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "study_location_tg",
                schema: "mmt",
                table: "admission_programs",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "tuition_fee_tjs",
                schema: "mmt",
                table: "admission_programs",
                type: "numeric(12,2)",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE mmt.universities
                SET normalized_full_name_ru = upper(regexp_replace(trim(COALESCE(NULLIF(full_name_ru, ''), full_name)), '\s+', ' ', 'g'));

                UPDATE mmt.admission_programs AS program
                SET study_location_tg = university.city,
                    study_location_ru = university.city_ru,
                    normalized_study_location = upper(regexp_replace(trim(COALESCE(NULLIF(university.city, ''), university.city_ru)), '\s+', ' ', 'g'))
                FROM mmt.universities AS university
                WHERE university.id = program.university_id;
                """);

            migrationBuilder.CreateIndex(
                name: "ux_mmt_universities_normalized_name_ru",
                schema: "mmt",
                table: "universities",
                column: "normalized_full_name_ru",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_mmt_admission_program_identity",
                schema: "mmt",
                table: "admission_programs",
                columns: new[] { "university_id", "specialty_id", "cluster_id", "admission_type", "study_form", "study_language", "admission_year", "normalized_study_location" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_mmt_program_budget_tuition",
                schema: "mmt",
                table: "admission_programs",
                sql: "admission_type <> 'Budget' OR tuition_fee_tjs IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_mmt_program_tuition",
                schema: "mmt",
                table: "admission_programs",
                sql: "tuition_fee_tjs IS NULL OR tuition_fee_tjs >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_mmt_universities_normalized_name_ru",
                schema: "mmt",
                table: "universities");

            migrationBuilder.DropIndex(
                name: "ux_mmt_admission_program_identity",
                schema: "mmt",
                table: "admission_programs");

            migrationBuilder.DropCheckConstraint(
                name: "ck_mmt_program_budget_tuition",
                schema: "mmt",
                table: "admission_programs");

            migrationBuilder.DropCheckConstraint(
                name: "ck_mmt_program_tuition",
                schema: "mmt",
                table: "admission_programs");

            migrationBuilder.DropColumn(
                name: "normalized_full_name_ru",
                schema: "mmt",
                table: "universities");

            migrationBuilder.DropColumn(
                name: "normalized_study_location",
                schema: "mmt",
                table: "admission_programs");

            migrationBuilder.DropColumn(
                name: "study_location_ru",
                schema: "mmt",
                table: "admission_programs");

            migrationBuilder.DropColumn(
                name: "study_location_tg",
                schema: "mmt",
                table: "admission_programs");

            migrationBuilder.DropColumn(
                name: "tuition_fee_tjs",
                schema: "mmt",
                table: "admission_programs");

            migrationBuilder.CreateIndex(
                name: "ux_mmt_admission_program_identity",
                schema: "mmt",
                table: "admission_programs",
                columns: new[] { "university_id", "specialty_id", "cluster_id", "admission_type", "study_form", "study_language", "admission_year" },
                unique: true);
        }
    }
}
