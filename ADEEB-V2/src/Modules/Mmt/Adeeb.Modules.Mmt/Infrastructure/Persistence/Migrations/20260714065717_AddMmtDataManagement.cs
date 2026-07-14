using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adeeb.Modules.Mmt.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMmtDataManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "mmt");

            migrationBuilder.CreateTable(
                name: "clusters",
                schema: "mmt",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clusters", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "specialties",
                schema: "mmt",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_specialties", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "universities",
                schema: "mmt",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    normalized_full_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    short_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    city = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    type = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    logo_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_universities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "admission_programs",
                schema: "mmt",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    university_id = table.Column<Guid>(type: "uuid", nullable: false),
                    specialty_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cluster_id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_type = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    study_form = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    study_language = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    admission_year = table.Column<int>(type: "integer", nullable: false),
                    seats_count = table.Column<int>(type: "integer", nullable: true),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admission_programs", x => x.id);
                    table.CheckConstraint("ck_mmt_program_seats", "seats_count IS NULL OR seats_count >= 0");
                    table.CheckConstraint("ck_mmt_program_year", "admission_year >= 2000 AND admission_year <= 2100");
                    table.ForeignKey(
                        name: "FK_admission_programs_clusters_cluster_id",
                        column: x => x.cluster_id,
                        principalSchema: "mmt",
                        principalTable: "clusters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_admission_programs_specialties_specialty_id",
                        column: x => x.specialty_id,
                        principalSchema: "mmt",
                        principalTable: "specialties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_admission_programs_universities_university_id",
                        column: x => x.university_id,
                        principalSchema: "mmt",
                        principalTable: "universities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "passing_score_history",
                schema: "mmt",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_program_id = table.Column<Guid>(type: "uuid", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    passing_score = table.Column<decimal>(type: "numeric", nullable: false),
                    seats_count = table.Column<int>(type: "integer", nullable: true),
                    source = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_passing_score_history", x => x.id);
                    table.CheckConstraint("ck_mmt_score_seats", "seats_count IS NULL OR seats_count >= 0");
                    table.CheckConstraint("ck_mmt_score_value", "passing_score > 0 AND passing_score <= 1000 AND scale(passing_score) <= 2");
                    table.CheckConstraint("ck_mmt_score_year", "year >= 2000 AND year <= 2100");
                    table.ForeignKey(
                        name: "FK_passing_score_history_admission_programs_admission_program_~",
                        column: x => x.admission_program_id,
                        principalSchema: "mmt",
                        principalTable: "admission_programs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_admission_programs_specialty_id",
                schema: "mmt",
                table: "admission_programs",
                column: "specialty_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmt_program_student_lookup",
                schema: "mmt",
                table: "admission_programs",
                columns: new[] { "cluster_id", "admission_year", "is_published", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ux_mmt_admission_program_identity",
                schema: "mmt",
                table: "admission_programs",
                columns: new[] { "university_id", "specialty_id", "cluster_id", "admission_type", "study_form", "study_language", "admission_year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mmt_clusters_active_name",
                schema: "mmt",
                table: "clusters",
                columns: new[] { "is_active", "name" });

            migrationBuilder.CreateIndex(
                name: "ux_mmt_clusters_code",
                schema: "mmt",
                table: "clusters",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mmt_score_latest",
                schema: "mmt",
                table: "passing_score_history",
                columns: new[] { "admission_program_id", "year" },
                unique: true,
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_mmt_specialties_active_name",
                schema: "mmt",
                table: "specialties",
                columns: new[] { "is_active", "name" });

            migrationBuilder.CreateIndex(
                name: "ux_mmt_specialties_code",
                schema: "mmt",
                table: "specialties",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mmt_universities_active_name",
                schema: "mmt",
                table: "universities",
                columns: new[] { "is_active", "full_name" });

            migrationBuilder.CreateIndex(
                name: "ux_mmt_universities_normalized_name",
                schema: "mmt",
                table: "universities",
                column: "normalized_full_name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "passing_score_history",
                schema: "mmt");

            migrationBuilder.DropTable(
                name: "admission_programs",
                schema: "mmt");

            migrationBuilder.DropTable(
                name: "clusters",
                schema: "mmt");

            migrationBuilder.DropTable(
                name: "specialties",
                schema: "mmt");

            migrationBuilder.DropTable(
                name: "universities",
                schema: "mmt");
        }
    }
}
