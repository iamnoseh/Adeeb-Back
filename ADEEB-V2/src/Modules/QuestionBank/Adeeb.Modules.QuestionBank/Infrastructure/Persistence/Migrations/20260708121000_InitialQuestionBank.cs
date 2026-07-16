using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adeeb.Modules.QuestionBank.Infrastructure.Persistence.Migrations;

[DbContext(typeof(QuestionBankDbContext))]
[Migration("20260708121000_InitialQuestionBank")]
public partial class InitialQuestionBank : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema("question_bank");

        migrationBuilder.CreateTable(
            name: "questions",
            schema: "question_bank",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                topic_id = table.Column<Guid>(type: "uuid", nullable: true),
                type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                difficulty = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                image_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                archived_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                archive_reason = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_questions", x => x.id));

        migrationBuilder.CreateTable(
            name: "answer_options",
            schema: "question_bank",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                question_id = table.Column<Guid>(type: "uuid", nullable: false),
                display_order = table.Column<int>(type: "integer", nullable: false),
                is_correct = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_answer_options", x => x.id);
                table.ForeignKey(
                    name: "fk_answer_options_questions_question_id",
                    column: x => x.question_id,
                    principalSchema: "question_bank",
                    principalTable: "questions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "question_translations",
            schema: "question_bank",
            columns: table => new
            {
                question_id = table.Column<Guid>(type: "uuid", nullable: false),
                language = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                explanation = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_question_translations", x => new { x.question_id, x.language });
                table.ForeignKey(
                    name: "fk_question_translations_questions_question_id",
                    column: x => x.question_id,
                    principalSchema: "question_bank",
                    principalTable: "questions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "answer_option_translations",
            schema: "question_bank",
            columns: table => new
            {
                answer_option_id = table.Column<Guid>(type: "uuid", nullable: false),
                language = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                match_pair_text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_answer_option_translations", x => new { x.answer_option_id, x.language });
                table.ForeignKey(
                    name: "fk_answer_option_translations_answer_options_answer_option_id",
                    column: x => x.answer_option_id,
                    principalSchema: "question_bank",
                    principalTable: "answer_options",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("ix_answer_options_question_id_display_order", "answer_options", new[] { "question_id", "display_order" }, "question_bank");
        migrationBuilder.CreateIndex("ix_questions_subject_id_status", "questions", new[] { "subject_id", "status" }, "question_bank");
        migrationBuilder.CreateIndex("ix_questions_topic_id_status", "questions", new[] { "topic_id", "status" }, "question_bank");
        migrationBuilder.CreateIndex("ix_questions_type_difficulty_status", "questions", new[] { "type", "difficulty", "status" }, "question_bank");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("answer_option_translations", "question_bank");
        migrationBuilder.DropTable("question_translations", "question_bank");
        migrationBuilder.DropTable("answer_options", "question_bank");
        migrationBuilder.DropTable("questions", "question_bank");
    }
}
