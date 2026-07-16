using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adeeb.Modules.QuestionBank.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentTestingDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "student_red_list_items",
                schema: "question_bank",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    topic_id = table.Column<Guid>(type: "uuid", nullable: true),
                    question_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    wrong_count = table.Column<int>(type: "integer", nullable: false),
                    correct_streak = table.Column<int>(type: "integer", nullable: false),
                    last_wrong_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_practiced_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    mastered_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_red_list_items", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "test_attempts",
                schema: "question_bank",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cluster_id = table.Column<Guid>(type: "uuid", nullable: true),
                    monthly_window_key = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    submitted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    question_count = table.Column<int>(type: "integer", nullable: false),
                    correct_count = table.Column<int>(type: "integer", nullable: false),
                    wrong_count = table.Column<int>(type: "integer", nullable: false),
                    score = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_test_attempts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "test_attempt_questions",
                schema: "question_bank",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    topic_id = table.Column<Guid>(type: "uuid", nullable: true),
                    question_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    difficulty = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    question_snapshot_json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_test_attempt_questions", x => x.id);
                    table.ForeignKey(
                        name: "FK_test_attempt_questions_test_attempts_test_attempt_id",
                        column: x => x.test_attempt_id,
                        principalSchema: "question_bank",
                        principalTable: "test_attempts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "test_attempt_results",
                schema: "question_bank",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    topic_breakdown_json = table.Column<string>(type: "jsonb", nullable: false),
                    result_snapshot_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_test_attempt_results", x => x.id);
                    table.ForeignKey(
                        name: "FK_test_attempt_results_test_attempts_test_attempt_id",
                        column: x => x.test_attempt_id,
                        principalSchema: "question_bank",
                        principalTable: "test_attempts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "test_attempt_answers",
                schema: "question_bank",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_attempt_question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    answer_snapshot_json = table.Column<string>(type: "jsonb", nullable: false),
                    is_answered = table.Column<bool>(type: "boolean", nullable: false),
                    is_correct = table.Column<bool>(type: "boolean", nullable: false),
                    correct_pairs_count = table.Column<int>(type: "integer", nullable: true),
                    total_pairs_count = table.Column<int>(type: "integer", nullable: true),
                    answered_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_test_attempt_answers", x => x.id);
                    table.ForeignKey(
                        name: "FK_test_attempt_answers_test_attempt_questions_test_attempt_qu~",
                        column: x => x.test_attempt_question_id,
                        principalSchema: "question_bank",
                        principalTable: "test_attempt_questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_test_attempt_answers_test_attempts_test_attempt_id",
                        column: x => x.test_attempt_id,
                        principalSchema: "question_bank",
                        principalTable: "test_attempts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_student_red_list_items_user_id_last_practiced_at_utc",
                schema: "question_bank",
                table: "student_red_list_items",
                columns: new[] { "user_id", "last_practiced_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_student_red_list_items_user_id_status_subject_id",
                schema: "question_bank",
                table: "student_red_list_items",
                columns: new[] { "user_id", "status", "subject_id" });

            migrationBuilder.CreateIndex(
                name: "ux_question_bank_red_list_user_question",
                schema: "question_bank",
                table: "student_red_list_items",
                columns: new[] { "user_id", "question_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_test_attempt_answers_test_attempt_id",
                schema: "question_bank",
                table: "test_attempt_answers",
                column: "test_attempt_id");

            migrationBuilder.CreateIndex(
                name: "ux_question_bank_attempt_answer_question",
                schema: "question_bank",
                table: "test_attempt_answers",
                column: "test_attempt_question_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_question_bank_attempt_question_identity",
                schema: "question_bank",
                table: "test_attempt_questions",
                columns: new[] { "test_attempt_id", "question_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_question_bank_attempt_question_order",
                schema: "question_bank",
                table: "test_attempt_questions",
                columns: new[] { "test_attempt_id", "display_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_question_bank_attempt_result",
                schema: "question_bank",
                table: "test_attempt_results",
                column: "test_attempt_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_test_attempts_user_id_started_at_utc",
                schema: "question_bank",
                table: "test_attempts",
                columns: new[] { "user_id", "started_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_test_attempts_user_id_status",
                schema: "question_bank",
                table: "test_attempts",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_question_bank_monthly_window",
                schema: "question_bank",
                table: "test_attempts",
                columns: new[] { "user_id", "mode", "monthly_window_key" },
                unique: true,
                filter: "monthly_window_key IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "student_red_list_items",
                schema: "question_bank");

            migrationBuilder.DropTable(
                name: "test_attempt_answers",
                schema: "question_bank");

            migrationBuilder.DropTable(
                name: "test_attempt_results",
                schema: "question_bank");

            migrationBuilder.DropTable(
                name: "test_attempt_questions",
                schema: "question_bank");

            migrationBuilder.DropTable(
                name: "test_attempts",
                schema: "question_bank");
        }
    }
}
