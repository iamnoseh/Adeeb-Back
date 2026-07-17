using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adeeb.Modules.Vocabulary.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialVocabulary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "vocabulary");

            migrationBuilder.CreateTable(
                name: "languages",
                schema: "vocabulary",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    name_tg = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    name_ru = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_languages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sessions",
                schema: "vocabulary",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    level = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    topic_id = table.Column<Guid>(type: "uuid", nullable: true),
                    local_date = table.Column<DateOnly>(type: "date", nullable: false),
                    question_count = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    correct_count = table.Column<int>(type: "integer", nullable: false),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sessions", x => x.id);
                    table.ForeignKey(
                        name: "FK_sessions_languages_language_id",
                        column: x => x.language_id,
                        principalSchema: "vocabulary",
                        principalTable: "languages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "student_courses",
                schema: "vocabulary",
                columns: table => new
                {
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_id = table.Column<Guid>(type: "uuid", nullable: false),
                    level = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_student_vocabulary_courses", x => x.student_id);
                    table.ForeignKey(
                        name: "FK_student_courses_languages_language_id",
                        column: x => x.language_id,
                        principalSchema: "vocabulary",
                        principalTable: "languages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "topics",
                schema: "vocabulary",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_id = table.Column<Guid>(type: "uuid", nullable: false),
                    level = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name_tg = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    name_ru = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    description_tg = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    description_ru = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_topics", x => x.id);
                    table.ForeignKey(
                        name: "FK_topics_languages_language_id",
                        column: x => x.language_id,
                        principalSchema: "vocabulary",
                        principalTable: "languages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "session_questions",
                schema: "vocabulary",
                columns: table => new
                {
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    word_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    prompt = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    correct_token_index = table.Column<int>(type: "integer", nullable: true),
                    options_json = table.Column<string>(type: "jsonb", nullable: false),
                    correct_answer_json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_session_questions", x => new { x.session_id, x.question_id });
                    table.ForeignKey(
                        name: "FK_session_questions_sessions_session_id",
                        column: x => x.session_id,
                        principalSchema: "vocabulary",
                        principalTable: "sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "words",
                schema: "vocabulary",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_id = table.Column<Guid>(type: "uuid", nullable: false),
                    topic_id = table.Column<Guid>(type: "uuid", nullable: false),
                    level = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    target_text = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    normalized_text = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    translation_tg = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    translation_ru = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    explanation_tg = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    explanation_ru = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    example_target = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    example_tg = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    example_ru = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_words", x => x.id);
                    table.ForeignKey(
                        name: "FK_words_languages_language_id",
                        column: x => x.language_id,
                        principalSchema: "vocabulary",
                        principalTable: "languages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_words_topics_topic_id",
                        column: x => x.topic_id,
                        principalSchema: "vocabulary",
                        principalTable: "topics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "session_answers",
                schema: "vocabulary",
                columns: table => new
                {
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    submission_json = table.Column<string>(type: "jsonb", nullable: false),
                    is_correct = table.Column<bool>(type: "boolean", nullable: false),
                    answered_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vocabulary_session_answers", x => new { x.session_id, x.question_id });
                    table.ForeignKey(
                        name: "FK_session_answers_session_questions_session_id_question_id",
                        columns: x => new { x.session_id, x.question_id },
                        principalSchema: "vocabulary",
                        principalTable: "session_questions",
                        principalColumns: new[] { "session_id", "question_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "daily_words",
                schema: "vocabulary",
                columns: table => new
                {
                    language_id = table.Column<Guid>(type: "uuid", nullable: false),
                    local_date = table.Column<DateOnly>(type: "date", nullable: false),
                    word_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_automatic = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vocabulary_daily_words", x => new { x.language_id, x.local_date });
                    table.ForeignKey(
                        name: "FK_daily_words_languages_language_id",
                        column: x => x.language_id,
                        principalSchema: "vocabulary",
                        principalTable: "languages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_daily_words_words_word_id",
                        column: x => x.word_id,
                        principalSchema: "vocabulary",
                        principalTable: "words",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "questions",
                schema: "vocabulary",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    word_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    prompt_target = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    prompt_tg = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    prompt_ru = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    correct_token_index = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    reviewed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_questions", x => x.id);
                    table.ForeignKey(
                        name: "FK_questions_words_word_id",
                        column: x => x.word_id,
                        principalSchema: "vocabulary",
                        principalTable: "words",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "relations",
                schema: "vocabulary",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    word_id = table.Column<Guid>(type: "uuid", nullable: false),
                    related_word_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_relations", x => x.id);
                    table.ForeignKey(
                        name: "FK_relations_words_related_word_id",
                        column: x => x.related_word_id,
                        principalSchema: "vocabulary",
                        principalTable: "words",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_relations_words_word_id",
                        column: x => x.word_id,
                        principalSchema: "vocabulary",
                        principalTable: "words",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "student_word_progress",
                schema: "vocabulary",
                columns: table => new
                {
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    word_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mastery_level = table.Column<int>(type: "integer", nullable: false),
                    correct_count = table.Column<int>(type: "integer", nullable: false),
                    wrong_count = table.Column<int>(type: "integer", nullable: false),
                    last_practiced_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    next_review_date = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_student_word_progress", x => new { x.student_id, x.word_id });
                    table.ForeignKey(
                        name: "FK_student_word_progress_words_word_id",
                        column: x => x.word_id,
                        principalSchema: "vocabulary",
                        principalTable: "words",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "question_options",
                schema: "vocabulary",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    word_id = table.Column<Guid>(type: "uuid", nullable: true),
                    value_target = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    value_tg = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    value_ru = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_correct = table.Column<bool>(type: "boolean", nullable: false),
                    correct_order = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_question_options", x => x.id);
                    table.ForeignKey(
                        name: "FK_question_options_questions_question_id",
                        column: x => x.question_id,
                        principalSchema: "vocabulary",
                        principalTable: "questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_question_options_words_word_id",
                        column: x => x.word_id,
                        principalSchema: "vocabulary",
                        principalTable: "words",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_daily_words_word_id",
                schema: "vocabulary",
                table: "daily_words",
                column: "word_id");

            migrationBuilder.CreateIndex(
                name: "ux_vocabulary_languages_code",
                schema: "vocabulary",
                table: "languages",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_question_options_question_id_display_order",
                schema: "vocabulary",
                table: "question_options",
                columns: new[] { "question_id", "display_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_question_options_word_id",
                schema: "vocabulary",
                table: "question_options",
                column: "word_id");

            migrationBuilder.CreateIndex(
                name: "IX_questions_word_id_type_status",
                schema: "vocabulary",
                table: "questions",
                columns: new[] { "word_id", "type", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_relations_related_word_id",
                schema: "vocabulary",
                table: "relations",
                column: "related_word_id");

            migrationBuilder.CreateIndex(
                name: "ux_vocabulary_relations_word_related_type",
                schema: "vocabulary",
                table: "relations",
                columns: new[] { "word_id", "related_word_id", "type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_session_questions_session_id_display_order",
                schema: "vocabulary",
                table: "session_questions",
                columns: new[] { "session_id", "display_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sessions_language_id",
                schema: "vocabulary",
                table: "sessions",
                column: "language_id");

            migrationBuilder.CreateIndex(
                name: "ix_vocabulary_sessions_student_mode_status",
                schema: "vocabulary",
                table: "sessions",
                columns: new[] { "student_id", "mode", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_student_courses_language_id",
                schema: "vocabulary",
                table: "student_courses",
                column: "language_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_word_progress_student_id_next_review_date",
                schema: "vocabulary",
                table: "student_word_progress",
                columns: new[] { "student_id", "next_review_date" });

            migrationBuilder.CreateIndex(
                name: "IX_student_word_progress_word_id",
                schema: "vocabulary",
                table: "student_word_progress",
                column: "word_id");

            migrationBuilder.CreateIndex(
                name: "IX_topics_language_id_level_status",
                schema: "vocabulary",
                table: "topics",
                columns: new[] { "language_id", "level", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_words_topic_id_level_status",
                schema: "vocabulary",
                table: "words",
                columns: new[] { "topic_id", "level", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_vocabulary_words_language_normalized_text",
                schema: "vocabulary",
                table: "words",
                columns: new[] { "language_id", "normalized_text" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_words",
                schema: "vocabulary");

            migrationBuilder.DropTable(
                name: "question_options",
                schema: "vocabulary");

            migrationBuilder.DropTable(
                name: "relations",
                schema: "vocabulary");

            migrationBuilder.DropTable(
                name: "session_answers",
                schema: "vocabulary");

            migrationBuilder.DropTable(
                name: "student_courses",
                schema: "vocabulary");

            migrationBuilder.DropTable(
                name: "student_word_progress",
                schema: "vocabulary");

            migrationBuilder.DropTable(
                name: "questions",
                schema: "vocabulary");

            migrationBuilder.DropTable(
                name: "session_questions",
                schema: "vocabulary");

            migrationBuilder.DropTable(
                name: "words",
                schema: "vocabulary");

            migrationBuilder.DropTable(
                name: "sessions",
                schema: "vocabulary");

            migrationBuilder.DropTable(
                name: "topics",
                schema: "vocabulary");

            migrationBuilder.DropTable(
                name: "languages",
                schema: "vocabulary");
        }
    }
}
