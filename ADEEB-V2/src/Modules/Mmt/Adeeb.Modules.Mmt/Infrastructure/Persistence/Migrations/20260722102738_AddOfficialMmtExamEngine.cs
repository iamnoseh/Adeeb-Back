using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adeeb.Modules.Mmt.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOfficialMmtExamEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "exam_versions",
                schema: "mmt",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_year = table.Column<int>(type: "integer", nullable: false),
                    name_tg = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    name_ru = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    is_official = table.Column<bool>(type: "boolean", nullable: false),
                    source_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    source_checksum = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    published_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exam_versions", x => x.id);
                    table.CheckConstraint("ck_mmt_exam_version_year", "admission_year BETWEEN 2000 AND 2100");
                });

            migrationBuilder.CreateTable(
                name: "exam_blueprints",
                schema: "mmt",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    exam_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cluster_id = table.Column<Guid>(type: "uuid", nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exam_blueprints", x => x.id);
                    table.CheckConstraint("ck_mmt_exam_blueprint_duration", "duration_minutes BETWEEN 30 AND 360");
                    table.ForeignKey(
                        name: "FK_exam_blueprints_clusters_cluster_id",
                        column: x => x.cluster_id,
                        principalSchema: "mmt",
                        principalTable: "clusters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_exam_blueprints_exam_versions_exam_version_id",
                        column: x => x.exam_version_id,
                        principalSchema: "mmt",
                        principalTable: "exam_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "exam_pass_thresholds",
                schema: "mmt",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    exam_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cluster_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subtest_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    minimum_raw_score = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exam_pass_thresholds", x => x.id);
                    table.CheckConstraint("ck_mmt_exam_threshold_raw", "minimum_raw_score BETWEEN 0 AND 40");
                    table.ForeignKey(
                        name: "FK_exam_pass_thresholds_exam_versions_exam_version_id",
                        column: x => x.exam_version_id,
                        principalSchema: "mmt",
                        principalTable: "exam_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "exam_specialty_ranges",
                schema: "mmt",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    exam_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cluster_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    a2_max_score = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    a3_max_score = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    a4_max_score = table.Column<decimal>(type: "numeric(9,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exam_specialty_ranges", x => x.id);
                    table.ForeignKey(
                        name: "FK_exam_specialty_ranges_clusters_cluster_id",
                        column: x => x.cluster_id,
                        principalSchema: "mmt",
                        principalTable: "clusters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_exam_specialty_ranges_exam_versions_exam_version_id",
                        column: x => x.exam_version_id,
                        principalSchema: "mmt",
                        principalTable: "exam_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "exam_subtests",
                schema: "mmt",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    exam_blueprint_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    single_choice_count = table.Column<int>(type: "integer", nullable: false),
                    matching_count = table.Column<int>(type: "integer", nullable: false),
                    short_answer_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exam_subtests", x => x.id);
                    table.CheckConstraint("ck_mmt_exam_subtest_counts", "single_choice_count >= 0 AND matching_count >= 0 AND short_answer_count >= 0");
                    table.CheckConstraint("ck_mmt_exam_subtest_order", "display_order BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "FK_exam_subtests_exam_blueprints_exam_blueprint_id",
                        column: x => x.exam_blueprint_id,
                        principalSchema: "mmt",
                        principalTable: "exam_blueprints",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "exam_score_scale",
                schema: "mmt",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    exam_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cluster_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subtest_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    specialty_range_id = table.Column<Guid>(type: "uuid", nullable: true),
                    raw_score = table.Column<int>(type: "integer", nullable: false),
                    scaled_score = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    max_scaled_score = table.Column<decimal>(type: "numeric(9,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exam_score_scale", x => x.id);
                    table.CheckConstraint("ck_mmt_exam_scale_raw", "raw_score BETWEEN 0 AND 40");
                    table.ForeignKey(
                        name: "FK_exam_score_scale_exam_specialty_ranges_specialty_range_id",
                        column: x => x.specialty_range_id,
                        principalSchema: "mmt",
                        principalTable: "exam_specialty_ranges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_exam_score_scale_exam_versions_exam_version_id",
                        column: x => x.exam_version_id,
                        principalSchema: "mmt",
                        principalTable: "exam_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "exam_specialty_range_specialties",
                schema: "mmt",
                columns: table => new
                {
                    specialty_range_id = table.Column<Guid>(type: "uuid", nullable: false),
                    specialty_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exam_specialty_range_specialties", x => new { x.specialty_range_id, x.specialty_id });
                    table.ForeignKey(
                        name: "FK_exam_specialty_range_specialties_exam_specialty_ranges_spec~",
                        column: x => x.specialty_range_id,
                        principalSchema: "mmt",
                        principalTable: "exam_specialty_ranges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_exam_specialty_range_specialties_specialties_specialty_id",
                        column: x => x.specialty_id,
                        principalSchema: "mmt",
                        principalTable: "specialties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_exam_blueprints_cluster_id",
                schema: "mmt",
                table: "exam_blueprints",
                column: "cluster_id");

            migrationBuilder.CreateIndex(
                name: "ux_mmt_exam_blueprints_version_cluster",
                schema: "mmt",
                table: "exam_blueprints",
                columns: new[] { "exam_version_id", "cluster_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_mmt_exam_threshold_identity",
                schema: "mmt",
                table: "exam_pass_thresholds",
                columns: new[] { "exam_version_id", "cluster_id", "subtest_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_exam_score_scale_specialty_range_id",
                schema: "mmt",
                table: "exam_score_scale",
                column: "specialty_range_id");

            migrationBuilder.CreateIndex(
                name: "ux_mmt_exam_scale_identity",
                schema: "mmt",
                table: "exam_score_scale",
                columns: new[] { "exam_version_id", "cluster_id", "subtest_code", "specialty_range_id", "raw_score" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_mmt_exam_range_specialty",
                schema: "mmt",
                table: "exam_specialty_range_specialties",
                column: "specialty_id");

            migrationBuilder.CreateIndex(
                name: "IX_exam_specialty_ranges_cluster_id",
                schema: "mmt",
                table: "exam_specialty_ranges",
                column: "cluster_id");

            migrationBuilder.CreateIndex(
                name: "ux_mmt_exam_ranges_version_cluster_code",
                schema: "mmt",
                table: "exam_specialty_ranges",
                columns: new[] { "exam_version_id", "cluster_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_mmt_exam_subtests_blueprint_code",
                schema: "mmt",
                table: "exam_subtests",
                columns: new[] { "exam_blueprint_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_mmt_exam_subtests_blueprint_order",
                schema: "mmt",
                table: "exam_subtests",
                columns: new[] { "exam_blueprint_id", "display_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_mmt_exam_versions_year_official",
                schema: "mmt",
                table: "exam_versions",
                columns: new[] { "admission_year", "is_official" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "exam_pass_thresholds",
                schema: "mmt");

            migrationBuilder.DropTable(
                name: "exam_score_scale",
                schema: "mmt");

            migrationBuilder.DropTable(
                name: "exam_specialty_range_specialties",
                schema: "mmt");

            migrationBuilder.DropTable(
                name: "exam_subtests",
                schema: "mmt");

            migrationBuilder.DropTable(
                name: "exam_specialty_ranges",
                schema: "mmt");

            migrationBuilder.DropTable(
                name: "exam_blueprints",
                schema: "mmt");

            migrationBuilder.DropTable(
                name: "exam_versions",
                schema: "mmt");
        }
    }
}
