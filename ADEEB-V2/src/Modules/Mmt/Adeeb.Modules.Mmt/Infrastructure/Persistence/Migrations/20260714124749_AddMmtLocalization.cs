using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adeeb.Modules.Mmt.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMmtLocalization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "city_ru",
                schema: "mmt",
                table: "universities",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "full_name_ru",
                schema: "mmt",
                table: "universities",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "short_name_ru",
                schema: "mmt",
                table: "universities",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description_ru",
                schema: "mmt",
                table: "specialties",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "name_ru",
                schema: "mmt",
                table: "specialties",
                type: "character varying(240)",
                maxLength: 240,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "description_ru",
                schema: "mmt",
                table: "clusters",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "name_ru",
                schema: "mmt",
                table: "clusters",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE mmt.clusters
                SET name_ru = name, description_ru = description;

                UPDATE mmt.specialties
                SET name_ru = name, description_ru = description;

                UPDATE mmt.universities
                SET full_name_ru = full_name, short_name_ru = short_name, city_ru = city;

                ALTER TABLE mmt.clusters ALTER COLUMN name_ru DROP DEFAULT;
                ALTER TABLE mmt.specialties ALTER COLUMN name_ru DROP DEFAULT;
                ALTER TABLE mmt.universities ALTER COLUMN full_name_ru DROP DEFAULT;
                ALTER TABLE mmt.universities ALTER COLUMN city_ru DROP DEFAULT;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "city_ru",
                schema: "mmt",
                table: "universities");

            migrationBuilder.DropColumn(
                name: "full_name_ru",
                schema: "mmt",
                table: "universities");

            migrationBuilder.DropColumn(
                name: "short_name_ru",
                schema: "mmt",
                table: "universities");

            migrationBuilder.DropColumn(
                name: "description_ru",
                schema: "mmt",
                table: "specialties");

            migrationBuilder.DropColumn(
                name: "name_ru",
                schema: "mmt",
                table: "specialties");

            migrationBuilder.DropColumn(
                name: "description_ru",
                schema: "mmt",
                table: "clusters");

            migrationBuilder.DropColumn(
                name: "name_ru",
                schema: "mmt",
                table: "clusters");
        }
    }
}
