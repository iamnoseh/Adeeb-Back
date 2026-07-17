using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adeeb.Modules.Vocabulary.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenVocabularySessionConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_vocabulary_sessions_student_mode_status",
                schema: "vocabulary",
                table: "sessions");

            migrationBuilder.CreateIndex(
                name: "ix_vocabulary_sessions_student_mode_status",
                schema: "vocabulary",
                table: "sessions",
                columns: new[] { "student_id", "language_id", "mode" },
                unique: true,
                filter: "status = 'InProgress'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_vocabulary_sessions_student_mode_status",
                schema: "vocabulary",
                table: "sessions");

            migrationBuilder.CreateIndex(
                name: "ix_vocabulary_sessions_student_mode_status",
                schema: "vocabulary",
                table: "sessions",
                columns: new[] { "student_id", "mode", "status" });
        }
    }
}
