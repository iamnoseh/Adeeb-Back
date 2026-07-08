using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adeeb.Modules.QuestionBank.Infrastructure.Persistence.Migrations;

[DbContext(typeof(QuestionBankDbContext))]
[Migration("20260708133000_AddLegacyTopicTextToQuestions")]
public partial class AddLegacyTopicTextToQuestions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "topic",
            schema: "question_bank",
            table: "questions",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "topic",
            schema: "question_bank",
            table: "questions");
    }
}
