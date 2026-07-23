using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adeeb.Modules.QuestionBank.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMmtAttemptSnapshotsAndDrafts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "mode_snapshot_json",
                schema: "question_bank",
                table: "test_attempts",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "official_score_snapshot_json",
                schema: "question_bank",
                table: "test_attempt_results",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "test_attempt_draft_answers",
                schema: "question_bank",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_attempt_question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    answer_snapshot_json = table.Column<string>(type: "jsonb", nullable: false),
                    is_marked_for_review = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_test_attempt_draft_answers", x => x.id);
                    table.ForeignKey(
                        name: "FK_test_attempt_draft_answers_test_attempt_questions_test_atte~",
                        column: x => x.test_attempt_question_id,
                        principalSchema: "question_bank",
                        principalTable: "test_attempt_questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_test_attempt_draft_answers_test_attempts_test_attempt_id",
                        column: x => x.test_attempt_id,
                        principalSchema: "question_bank",
                        principalTable: "test_attempts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_test_attempt_draft_answers_test_attempt_id",
                schema: "question_bank",
                table: "test_attempt_draft_answers",
                column: "test_attempt_id");

            migrationBuilder.CreateIndex(
                name: "ux_question_bank_attempt_draft_question",
                schema: "question_bank",
                table: "test_attempt_draft_answers",
                column: "test_attempt_question_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "test_attempt_draft_answers",
                schema: "question_bank");

            migrationBuilder.DropColumn(
                name: "mode_snapshot_json",
                schema: "question_bank",
                table: "test_attempts");

            migrationBuilder.DropColumn(
                name: "official_score_snapshot_json",
                schema: "question_bank",
                table: "test_attempt_results");
        }
    }
}
