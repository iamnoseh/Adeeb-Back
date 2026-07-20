using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adeeb.Modules.QuestionBank.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTestXpRewards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "student_test_xp_balances",
                schema: "question_bank",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_xp_units = table.Column<long>(type: "bigint", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_test_xp_balances", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "test_xp_rewards",
                schema: "question_bank",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    reward_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    easy_correct_count = table.Column<int>(type: "integer", nullable: false),
                    medium_correct_count = table.Column<int>(type: "integer", nullable: false),
                    hard_correct_count = table.Column<int>(type: "integer", nullable: false),
                    answer_xp_units = table.Column<int>(type: "integer", nullable: false),
                    completion_bonus_xp_units = table.Column<int>(type: "integer", nullable: false),
                    total_xp_units = table.Column<int>(type: "integer", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_test_xp_rewards", x => x.id);
                    table.ForeignKey(
                        name: "FK_test_xp_rewards_test_attempts_attempt_id",
                        column: x => x.attempt_id,
                        principalSchema: "question_bank",
                        principalTable: "test_attempts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_test_xp_rewards_user_id_created_at_utc",
                schema: "question_bank",
                table: "test_xp_rewards",
                columns: new[] { "user_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_question_bank_xp_reward_attempt",
                schema: "question_bank",
                table: "test_xp_rewards",
                column: "attempt_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_question_bank_xp_reward_idempotency",
                schema: "question_bank",
                table: "test_xp_rewards",
                column: "idempotency_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "student_test_xp_balances",
                schema: "question_bank");

            migrationBuilder.DropTable(
                name: "test_xp_rewards",
                schema: "question_bank");
        }
    }
}
