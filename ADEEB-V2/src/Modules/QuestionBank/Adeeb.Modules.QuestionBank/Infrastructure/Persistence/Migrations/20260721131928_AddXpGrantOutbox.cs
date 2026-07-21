using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adeeb.Modules.QuestionBank.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddXpGrantOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "xp_grant_outbox",
                schema: "question_bank",
                columns: table => new
                {
                    ledger_entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    entry_type = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    amount_units = table.Column<int>(type: "integer", nullable: false),
                    new_balance_units = table.Column<long>(type: "bigint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_xp_grant_outbox", x => x.ledger_entry_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_question_bank_xp_outbox_pending",
                schema: "question_bank",
                table: "xp_grant_outbox",
                columns: new[] { "processed_at_utc", "created_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "xp_grant_outbox",
                schema: "question_bank");
        }
    }
}
