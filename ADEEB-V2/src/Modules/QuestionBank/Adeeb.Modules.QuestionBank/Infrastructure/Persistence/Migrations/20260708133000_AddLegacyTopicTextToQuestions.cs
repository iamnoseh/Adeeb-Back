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
        migrationBuilder.Sql(
            """
            ALTER TABLE question_bank.questions
            ADD COLUMN IF NOT EXISTS topic character varying(200);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE question_bank.questions
            DROP COLUMN IF EXISTS topic;
            """);
    }
}
