using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adeeb.Modules.Mmt.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMmtDistributionRound : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_mmt_score_latest",
                schema: "mmt",
                table: "passing_score_history");

            migrationBuilder.AddColumn<string>(
                name: "distribution_round",
                schema: "mmt",
                table: "passing_score_history",
                type: "character varying(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "Main");

            migrationBuilder.CreateIndex(
                name: "ix_mmt_score_latest_round",
                schema: "mmt",
                table: "passing_score_history",
                columns: new[] { "admission_program_id", "distribution_round", "year" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ux_mmt_score_program_year_round",
                schema: "mmt",
                table: "passing_score_history",
                columns: new[] { "admission_program_id", "year", "distribution_round" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_mmt_score_latest_round",
                schema: "mmt",
                table: "passing_score_history");

            migrationBuilder.DropIndex(
                name: "ux_mmt_score_program_year_round",
                schema: "mmt",
                table: "passing_score_history");

            migrationBuilder.DropColumn(
                name: "distribution_round",
                schema: "mmt",
                table: "passing_score_history");

            migrationBuilder.CreateIndex(
                name: "ix_mmt_score_latest",
                schema: "mmt",
                table: "passing_score_history",
                columns: new[] { "admission_program_id", "year" },
                unique: true,
                descending: new[] { false, true });
        }
    }
}
