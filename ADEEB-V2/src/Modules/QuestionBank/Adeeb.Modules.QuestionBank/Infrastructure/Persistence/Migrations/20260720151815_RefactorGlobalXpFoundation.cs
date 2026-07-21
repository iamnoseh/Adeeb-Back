using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adeeb.Modules.QuestionBank.Infrastructure.Persistence.Migrations;

public partial class RefactorGlobalXpFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameTable(
            name: "student_test_xp_balances",
            schema: "question_bank",
            newName: "student_xp_balances",
            newSchema: "question_bank");
        migrationBuilder.Sql("""
            ALTER TABLE question_bank.student_xp_balances
                RENAME CONSTRAINT "PK_student_test_xp_balances" TO "PK_student_xp_balances";
            ALTER TABLE question_bank.student_xp_balances
                ADD CONSTRAINT ck_question_bank_xp_balance_non_negative CHECK (total_xp_units >= 0);
            """);

        migrationBuilder.CreateTable(
            name: "xp_ledger_entries",
            schema: "question_bank",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                source_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                source_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                entry_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                amount_units = table.Column<int>(type: "integer", nullable: false),
                idempotency_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                balance_before_units = table.Column<long>(type: "bigint", nullable: false),
                balance_after_units = table.Column<long>(type: "bigint", nullable: false),
                metadata_json = table.Column<string>(type: "jsonb", nullable: true),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_xp_ledger_entries", x => x.id);
                table.CheckConstraint("ck_question_bank_xp_ledger_amount_non_negative", "amount_units >= 0");
            });

        migrationBuilder.Sql("""
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM question_bank.test_xp_rewards
                    GROUP BY idempotency_key HAVING count(*) > 1
                ) OR EXISTS (
                    SELECT 1 FROM question_bank.test_xp_rewards
                    GROUP BY user_id, attempt_id HAVING count(*) > 1
                ) THEN
                    RAISE EXCEPTION 'Cannot migrate duplicate test XP rewards to the global ledger.';
                END IF;
            END $$;

            INSERT INTO question_bank.xp_ledger_entries
                (id, user_id, source_type, source_id, entry_type, amount_units, idempotency_key,
                 balance_before_units, balance_after_units, metadata_json, created_at_utc)
            SELECT
                reward.id,
                reward.user_id,
                'TestAttempt',
                replace(reward.attempt_id::text, '-', ''),
                CASE WHEN reward.total_xp_units > 0 THEN 'Credit' ELSE 'Settlement' END,
                reward.total_xp_units,
                reward.idempotency_key,
                cumulative.total_after - reward.total_xp_units,
                cumulative.total_after,
                jsonb_build_object('attemptId', replace(reward.attempt_id::text, '-', ''), 'testMode', reward.test_mode),
                reward.created_at_utc
            FROM question_bank.test_xp_rewards reward
            CROSS JOIN LATERAL (
                SELECT sum(previous.total_xp_units)::bigint AS total_after
                FROM question_bank.test_xp_rewards previous
                WHERE previous.user_id = reward.user_id
                  AND (previous.created_at_utc, previous.id) <= (reward.created_at_utc, reward.id)
            ) cumulative;

            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM question_bank.student_xp_balances balance
                    LEFT JOIN (
                        SELECT user_id, sum(amount_units)::bigint AS ledger_total
                        FROM question_bank.xp_ledger_entries
                        GROUP BY user_id
                    ) ledger ON ledger.user_id = balance.user_id
                    WHERE balance.total_xp_units <> COALESCE(ledger.ledger_total, 0)
                ) OR EXISTS (
                    SELECT 1
                    FROM (
                        SELECT user_id, sum(amount_units)::bigint AS ledger_total
                        FROM question_bank.xp_ledger_entries
                        GROUP BY user_id
                    ) ledger
                    LEFT JOIN question_bank.student_xp_balances balance ON balance.user_id = ledger.user_id
                    WHERE ledger.ledger_total > 0
                      AND balance.user_id IS NULL
                ) THEN
                    RAISE EXCEPTION 'Existing XP balances do not match test reward totals; migration stopped without changing balances.';
                END IF;
            END $$;
            """);

        migrationBuilder.DropForeignKey(
            name: "FK_test_xp_rewards_test_attempts_attempt_id",
            schema: "question_bank",
            table: "test_xp_rewards");
        migrationBuilder.DropPrimaryKey(
            name: "PK_test_xp_rewards",
            schema: "question_bank",
            table: "test_xp_rewards");
        migrationBuilder.DropIndex(
            name: "IX_test_xp_rewards_user_id_created_at_utc",
            schema: "question_bank",
            table: "test_xp_rewards");
        migrationBuilder.DropIndex(
            name: "ux_question_bank_xp_reward_attempt",
            schema: "question_bank",
            table: "test_xp_rewards");
        migrationBuilder.DropIndex(
            name: "ux_question_bank_xp_reward_idempotency",
            schema: "question_bank",
            table: "test_xp_rewards");

        migrationBuilder.RenameTable(
            name: "test_xp_rewards",
            schema: "question_bank",
            newName: "test_xp_settlements",
            newSchema: "question_bank");
        migrationBuilder.AddColumn<Guid>(
            name: "ledger_entry_id",
            schema: "question_bank",
            table: "test_xp_settlements",
            type: "uuid",
            nullable: true);
        migrationBuilder.Sql("UPDATE question_bank.test_xp_settlements SET ledger_entry_id = id;");
        migrationBuilder.AlterColumn<Guid>(
            name: "ledger_entry_id",
            schema: "question_bank",
            table: "test_xp_settlements",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);
        migrationBuilder.DropColumn(name: "user_id", schema: "question_bank", table: "test_xp_settlements");
        migrationBuilder.DropColumn(name: "test_mode", schema: "question_bank", table: "test_xp_settlements");
        migrationBuilder.DropColumn(name: "reward_type", schema: "question_bank", table: "test_xp_settlements");
        migrationBuilder.DropColumn(name: "idempotency_key", schema: "question_bank", table: "test_xp_settlements");

        migrationBuilder.AddPrimaryKey(
            name: "PK_test_xp_settlements",
            schema: "question_bank",
            table: "test_xp_settlements",
            column: "id");
        migrationBuilder.AddCheckConstraint(
            name: "ck_question_bank_xp_settlement_non_negative",
            schema: "question_bank",
            table: "test_xp_settlements",
            sql: "easy_correct_count >= 0 AND medium_correct_count >= 0 AND hard_correct_count >= 0 AND answer_xp_units >= 0 AND completion_bonus_xp_units >= 0 AND total_xp_units >= 0");
        migrationBuilder.AddCheckConstraint(
            name: "ck_question_bank_xp_settlement_total",
            schema: "question_bank",
            table: "test_xp_settlements",
            sql: "total_xp_units = answer_xp_units + completion_bonus_xp_units");
        migrationBuilder.AddCheckConstraint(
            name: "ck_question_bank_xp_settlement_zero_correct",
            schema: "question_bank",
            table: "test_xp_settlements",
            sql: "easy_correct_count + medium_correct_count + hard_correct_count > 0 OR total_xp_units = 0");

        migrationBuilder.CreateIndex(
            name: "ux_question_bank_xp_settlement_attempt",
            schema: "question_bank",
            table: "test_xp_settlements",
            column: "attempt_id",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "ux_question_bank_xp_settlement_ledger",
            schema: "question_bank",
            table: "test_xp_settlements",
            column: "ledger_entry_id",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "ix_question_bank_xp_ledger_user_history",
            schema: "question_bank",
            table: "xp_ledger_entries",
            columns: new[] { "user_id", "created_at_utc" });
        migrationBuilder.CreateIndex(
            name: "ix_question_bank_xp_ledger_user_source",
            schema: "question_bank",
            table: "xp_ledger_entries",
            columns: new[] { "user_id", "source_type" });
        migrationBuilder.CreateIndex(
            name: "ux_question_bank_xp_ledger_idempotency",
            schema: "question_bank",
            table: "xp_ledger_entries",
            column: "idempotency_key",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "ux_question_bank_xp_ledger_source",
            schema: "question_bank",
            table: "xp_ledger_entries",
            columns: new[] { "user_id", "source_type", "source_id" },
            unique: true);
        migrationBuilder.AddForeignKey(
            name: "FK_test_xp_settlements_test_attempts_attempt_id",
            schema: "question_bank",
            table: "test_xp_settlements",
            column: "attempt_id",
            principalSchema: "question_bank",
            principalTable: "test_attempts",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(
            name: "FK_test_xp_settlements_xp_ledger_entries_ledger_entry_id",
            schema: "question_bank",
            table: "test_xp_settlements",
            column: "ledger_entry_id",
            principalSchema: "question_bank",
            principalTable: "xp_ledger_entries",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $$
            BEGIN
                IF EXISTS (SELECT 1 FROM question_bank.xp_ledger_entries WHERE source_type <> 'TestAttempt') THEN
                    RAISE EXCEPTION 'Global XP rollback is blocked because non-test XP ledger entries exist.';
                END IF;
            END $$;
            """);

        migrationBuilder.DropForeignKey(name: "FK_test_xp_settlements_test_attempts_attempt_id", schema: "question_bank", table: "test_xp_settlements");
        migrationBuilder.DropForeignKey(name: "FK_test_xp_settlements_xp_ledger_entries_ledger_entry_id", schema: "question_bank", table: "test_xp_settlements");
        migrationBuilder.DropPrimaryKey(name: "PK_test_xp_settlements", schema: "question_bank", table: "test_xp_settlements");
        migrationBuilder.DropCheckConstraint(name: "ck_question_bank_xp_settlement_non_negative", schema: "question_bank", table: "test_xp_settlements");
        migrationBuilder.DropCheckConstraint(name: "ck_question_bank_xp_settlement_total", schema: "question_bank", table: "test_xp_settlements");
        migrationBuilder.DropCheckConstraint(name: "ck_question_bank_xp_settlement_zero_correct", schema: "question_bank", table: "test_xp_settlements");
        migrationBuilder.DropIndex(name: "ux_question_bank_xp_settlement_attempt", schema: "question_bank", table: "test_xp_settlements");
        migrationBuilder.DropIndex(name: "ux_question_bank_xp_settlement_ledger", schema: "question_bank", table: "test_xp_settlements");

        migrationBuilder.AddColumn<Guid>(name: "user_id", schema: "question_bank", table: "test_xp_settlements", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<string>(name: "test_mode", schema: "question_bank", table: "test_xp_settlements", type: "character varying(32)", maxLength: 32, nullable: true);
        migrationBuilder.AddColumn<string>(name: "reward_type", schema: "question_bank", table: "test_xp_settlements", type: "character varying(32)", maxLength: 32, nullable: true);
        migrationBuilder.AddColumn<string>(name: "idempotency_key", schema: "question_bank", table: "test_xp_settlements", type: "character varying(64)", maxLength: 64, nullable: true);
        migrationBuilder.Sql("""
            UPDATE question_bank.test_xp_settlements settlement
            SET user_id = ledger.user_id,
                test_mode = attempt.mode,
                reward_type = 'AttemptCompletion',
                idempotency_key = ledger.idempotency_key
            FROM question_bank.xp_ledger_entries ledger,
                 question_bank.test_attempts attempt
            WHERE settlement.ledger_entry_id = ledger.id
              AND settlement.attempt_id = attempt.id;
            """);
        migrationBuilder.AlterColumn<Guid>(name: "user_id", schema: "question_bank", table: "test_xp_settlements", type: "uuid", nullable: false, oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);
        migrationBuilder.AlterColumn<string>(name: "test_mode", schema: "question_bank", table: "test_xp_settlements", type: "character varying(32)", maxLength: 32, nullable: false, oldClrType: typeof(string), oldType: "character varying(32)", oldMaxLength: 32, oldNullable: true);
        migrationBuilder.AlterColumn<string>(name: "reward_type", schema: "question_bank", table: "test_xp_settlements", type: "character varying(32)", maxLength: 32, nullable: false, oldClrType: typeof(string), oldType: "character varying(32)", oldMaxLength: 32, oldNullable: true);
        migrationBuilder.AlterColumn<string>(name: "idempotency_key", schema: "question_bank", table: "test_xp_settlements", type: "character varying(64)", maxLength: 64, nullable: false, oldClrType: typeof(string), oldType: "character varying(64)", oldMaxLength: 64, oldNullable: true);
        migrationBuilder.DropColumn(name: "ledger_entry_id", schema: "question_bank", table: "test_xp_settlements");

        migrationBuilder.RenameTable(name: "test_xp_settlements", schema: "question_bank", newName: "test_xp_rewards", newSchema: "question_bank");
        migrationBuilder.AddPrimaryKey(name: "PK_test_xp_rewards", schema: "question_bank", table: "test_xp_rewards", column: "id");
        migrationBuilder.CreateIndex(name: "IX_test_xp_rewards_user_id_created_at_utc", schema: "question_bank", table: "test_xp_rewards", columns: new[] { "user_id", "created_at_utc" });
        migrationBuilder.CreateIndex(name: "ux_question_bank_xp_reward_attempt", schema: "question_bank", table: "test_xp_rewards", column: "attempt_id", unique: true);
        migrationBuilder.CreateIndex(name: "ux_question_bank_xp_reward_idempotency", schema: "question_bank", table: "test_xp_rewards", column: "idempotency_key", unique: true);
        migrationBuilder.AddForeignKey(name: "FK_test_xp_rewards_test_attempts_attempt_id", schema: "question_bank", table: "test_xp_rewards", column: "attempt_id", principalSchema: "question_bank", principalTable: "test_attempts", principalColumn: "id", onDelete: ReferentialAction.Cascade);

        migrationBuilder.DropTable(name: "xp_ledger_entries", schema: "question_bank");
        migrationBuilder.Sql("ALTER TABLE question_bank.student_xp_balances DROP CONSTRAINT ck_question_bank_xp_balance_non_negative;");
        migrationBuilder.RenameTable(name: "student_xp_balances", schema: "question_bank", newName: "student_test_xp_balances", newSchema: "question_bank");
        migrationBuilder.Sql("ALTER TABLE question_bank.student_test_xp_balances RENAME CONSTRAINT \"PK_student_xp_balances\" TO \"PK_student_test_xp_balances\";");
    }
}
