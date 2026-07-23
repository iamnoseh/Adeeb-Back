using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adeeb.Modules.Mmt.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMmtClusterSubjectOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "display_order",
                schema: "mmt",
                table: "cluster_subjects",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                WITH ordered AS (
                    SELECT cluster_id, subject_id,
                           ROW_NUMBER() OVER (PARTITION BY cluster_id ORDER BY subject_id) AS display_order
                    FROM mmt.cluster_subjects
                )
                UPDATE mmt.cluster_subjects AS target
                SET display_order = ordered.display_order
                FROM ordered
                WHERE target.cluster_id = ordered.cluster_id
                  AND target.subject_id = ordered.subject_id;

                WITH blueprint_order AS (
                    SELECT DISTINCT ON (blueprint.cluster_id, subtest.subject_id)
                           blueprint.cluster_id, subtest.subject_id, subtest.display_order
                    FROM mmt.exam_subtests AS subtest
                    INNER JOIN mmt.exam_blueprints AS blueprint
                        ON blueprint.id = subtest.exam_blueprint_id
                    INNER JOIN mmt.exam_versions AS version
                        ON version.id = blueprint.exam_version_id
                    ORDER BY blueprint.cluster_id, subtest.subject_id,
                             version.admission_year DESC, version.is_official DESC
                )
                UPDATE mmt.cluster_subjects AS target
                SET display_order = blueprint_order.display_order
                FROM blueprint_order
                WHERE target.cluster_id = blueprint_order.cluster_id
                  AND target.subject_id = blueprint_order.subject_id;
                """);

            migrationBuilder.CreateIndex(
                name: "ux_mmt_cluster_subjects_cluster_order",
                schema: "mmt",
                table: "cluster_subjects",
                columns: new[] { "cluster_id", "display_order" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_mmt_cluster_subjects_cluster_order",
                schema: "mmt",
                table: "cluster_subjects");

            migrationBuilder.DropColumn(
                name: "display_order",
                schema: "mmt",
                table: "cluster_subjects");
        }
    }
}
