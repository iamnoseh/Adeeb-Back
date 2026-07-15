using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adeeb.Modules.Mmt.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMmtSimulatorPhase2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "student_profiles",
                schema: "mmt",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cluster_id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_year = table.Column<int>(type: "integer", nullable: false),
                    goal_admission_program_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_profiles", x => x.id);
                    table.CheckConstraint("ck_mmt_student_profile_year", "admission_year >= 2000 AND admission_year <= 2100");
                    table.ForeignKey(
                        name: "FK_student_profiles_admission_programs_goal_admission_program_~",
                        column: x => x.goal_admission_program_id,
                        principalSchema: "mmt",
                        principalTable: "admission_programs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_student_profiles_clusters_cluster_id",
                        column: x => x.cluster_id,
                        principalSchema: "mmt",
                        principalTable: "clusters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "exam_evaluations",
                schema: "mmt",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_mmt_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exam_session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    total_score = table.Column<decimal>(type: "numeric", nullable: false),
                    admission_year = table.Column<int>(type: "integer", nullable: false),
                    cluster_id = table.Column<Guid>(type: "uuid", nullable: false),
                    evaluated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    accepted_choice_priority = table.Column<int>(type: "integer", nullable: true),
                    accepted_admission_program_id = table.Column<Guid>(type: "uuid", nullable: true),
                    missing_score_for_goal = table.Column<decimal>(type: "numeric", nullable: true),
                    readiness_percentage = table.Column<decimal>(type: "numeric", nullable: true),
                    motivational_message_key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exam_evaluations", x => x.id);
                    table.CheckConstraint("ck_mmt_evaluation_accepted_pair", "(accepted_choice_priority IS NULL) = (accepted_admission_program_id IS NULL)");
                    table.CheckConstraint("ck_mmt_evaluation_accepted_priority", "accepted_choice_priority IS NULL OR (accepted_choice_priority >= 1 AND accepted_choice_priority <= 12)");
                    table.CheckConstraint("ck_mmt_evaluation_goal_missing", "missing_score_for_goal IS NULL OR (missing_score_for_goal >= 0 AND missing_score_for_goal <= 1000 AND scale(missing_score_for_goal) <= 2)");
                    table.CheckConstraint("ck_mmt_evaluation_readiness", "readiness_percentage IS NULL OR (readiness_percentage >= 0 AND readiness_percentage <= 100 AND scale(readiness_percentage) <= 2)");
                    table.CheckConstraint("ck_mmt_evaluation_score", "total_score >= 0 AND total_score <= 1000 AND scale(total_score) <= 2");
                    table.CheckConstraint("ck_mmt_evaluation_year", "admission_year >= 2000 AND admission_year <= 2100");
                    table.ForeignKey(
                        name: "FK_exam_evaluations_admission_programs_accepted_admission_prog~",
                        column: x => x.accepted_admission_program_id,
                        principalSchema: "mmt",
                        principalTable: "admission_programs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_exam_evaluations_clusters_cluster_id",
                        column: x => x.cluster_id,
                        principalSchema: "mmt",
                        principalTable: "clusters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_exam_evaluations_student_profiles_student_mmt_profile_id",
                        column: x => x.student_mmt_profile_id,
                        principalSchema: "mmt",
                        principalTable: "student_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "student_admission_choices",
                schema: "mmt",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_mmt_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_program_id = table.Column<Guid>(type: "uuid", nullable: false),
                    priority_order = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_admission_choices", x => x.id);
                    table.CheckConstraint("ck_mmt_choice_priority", "priority_order >= 1 AND priority_order <= 12");
                    table.ForeignKey(
                        name: "FK_student_admission_choices_admission_programs_admission_prog~",
                        column: x => x.admission_program_id,
                        principalSchema: "mmt",
                        principalTable: "admission_programs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_student_admission_choices_student_profiles_student_mmt_prof~",
                        column: x => x.student_mmt_profile_id,
                        principalSchema: "mmt",
                        principalTable: "student_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "admission_choice_snapshots",
                schema: "mmt",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    mmt_exam_evaluation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    priority_order = table.Column<int>(type: "integer", nullable: false),
                    admission_program_id = table.Column<Guid>(type: "uuid", nullable: false),
                    university_name_snapshot = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    specialty_code_snapshot = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    specialty_name_snapshot = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    cluster_code_snapshot = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    admission_type = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    study_form = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    study_language = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    admission_year = table.Column<int>(type: "integer", nullable: false),
                    passing_score_used = table.Column<decimal>(type: "numeric", nullable: true),
                    conservative_threshold_used = table.Column<decimal>(type: "numeric", nullable: true),
                    student_score = table.Column<decimal>(type: "numeric", nullable: false),
                    is_accepted = table.Column<bool>(type: "boolean", nullable: false),
                    missing_score = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admission_choice_snapshots", x => x.id);
                    table.CheckConstraint("ck_mmt_snapshot_missing", "missing_score IS NULL OR (missing_score >= 0 AND missing_score <= 1000 AND scale(missing_score) <= 2)");
                    table.CheckConstraint("ck_mmt_snapshot_passing_score", "passing_score_used IS NULL OR (passing_score_used > 0 AND passing_score_used <= 1000 AND scale(passing_score_used) <= 2)");
                    table.CheckConstraint("ck_mmt_snapshot_priority", "priority_order >= 1 AND priority_order <= 12");
                    table.CheckConstraint("ck_mmt_snapshot_student_score", "student_score >= 0 AND student_score <= 1000 AND scale(student_score) <= 2");
                    table.CheckConstraint("ck_mmt_snapshot_threshold", "conservative_threshold_used IS NULL OR (conservative_threshold_used > 0 AND conservative_threshold_used <= 1000 AND scale(conservative_threshold_used) <= 2)");
                    table.CheckConstraint("ck_mmt_snapshot_year", "admission_year >= 2000 AND admission_year <= 2100");
                    table.ForeignKey(
                        name: "FK_admission_choice_snapshots_admission_programs_admission_pro~",
                        column: x => x.admission_program_id,
                        principalSchema: "mmt",
                        principalTable: "admission_programs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_admission_choice_snapshots_exam_evaluations_mmt_exam_evalua~",
                        column: x => x.mmt_exam_evaluation_id,
                        principalSchema: "mmt",
                        principalTable: "exam_evaluations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_admission_choice_snapshots_admission_program_id",
                schema: "mmt",
                table: "admission_choice_snapshots",
                column: "admission_program_id");

            migrationBuilder.CreateIndex(
                name: "ux_mmt_snapshot_evaluation_priority",
                schema: "mmt",
                table: "admission_choice_snapshots",
                columns: new[] { "mmt_exam_evaluation_id", "priority_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_exam_evaluations_accepted_admission_program_id",
                schema: "mmt",
                table: "exam_evaluations",
                column: "accepted_admission_program_id");

            migrationBuilder.CreateIndex(
                name: "IX_exam_evaluations_cluster_id",
                schema: "mmt",
                table: "exam_evaluations",
                column: "cluster_id");

            migrationBuilder.CreateIndex(
                name: "IX_exam_evaluations_student_mmt_profile_id",
                schema: "mmt",
                table: "exam_evaluations",
                column: "student_mmt_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmt_evaluations_admin_history",
                schema: "mmt",
                table: "exam_evaluations",
                columns: new[] { "admission_year", "evaluated_at_utc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_mmt_evaluations_user_history",
                schema: "mmt",
                table: "exam_evaluations",
                columns: new[] { "user_id", "evaluated_at_utc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_student_admission_choices_admission_program_id",
                schema: "mmt",
                table: "student_admission_choices",
                column: "admission_program_id");

            migrationBuilder.CreateIndex(
                name: "ux_mmt_choice_profile_priority",
                schema: "mmt",
                table: "student_admission_choices",
                columns: new[] { "student_mmt_profile_id", "priority_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_mmt_choice_profile_program",
                schema: "mmt",
                table: "student_admission_choices",
                columns: new[] { "student_mmt_profile_id", "admission_program_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mmt_student_profiles_admin",
                schema: "mmt",
                table: "student_profiles",
                columns: new[] { "cluster_id", "admission_year", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_student_profiles_goal_admission_program_id",
                schema: "mmt",
                table: "student_profiles",
                column: "goal_admission_program_id");

            migrationBuilder.CreateIndex(
                name: "ux_mmt_student_profile_active_year",
                schema: "mmt",
                table: "student_profiles",
                columns: new[] { "user_id", "admission_year" },
                unique: true,
                filter: "is_active");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admission_choice_snapshots",
                schema: "mmt");

            migrationBuilder.DropTable(
                name: "student_admission_choices",
                schema: "mmt");

            migrationBuilder.DropTable(
                name: "exam_evaluations",
                schema: "mmt");

            migrationBuilder.DropTable(
                name: "student_profiles",
                schema: "mmt");
        }
    }
}
