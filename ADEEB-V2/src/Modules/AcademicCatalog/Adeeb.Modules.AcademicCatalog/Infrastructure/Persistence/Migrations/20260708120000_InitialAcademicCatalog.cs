using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adeeb.Modules.AcademicCatalog.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AcademicCatalogDbContext))]
[Migration("20260708120000_InitialAcademicCatalog")]
public partial class InitialAcademicCatalog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema("academic");

        migrationBuilder.CreateTable(
            name: "subjects",
            schema: "academic",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                icon_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                display_order = table.Column<int>(type: "integer", nullable: false),
                status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                archived_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                archive_reason = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true)
            },
            constraints: table => table.PrimaryKey("pk_subjects", x => x.id));

        migrationBuilder.CreateTable(
            name: "subject_translations",
            schema: "academic",
            columns: table => new
            {
                subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                language = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_subject_translations", x => new { x.subject_id, x.language });
                table.ForeignKey(
                    name: "fk_subject_translations_subjects_subject_id",
                    column: x => x.subject_id,
                    principalSchema: "academic",
                    principalTable: "subjects",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "topics",
            schema: "academic",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                display_order = table.Column<int>(type: "integer", nullable: false),
                status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                archived_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                archive_reason = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_topics", x => x.id);
                table.ForeignKey(
                    name: "fk_topics_subjects_subject_id",
                    column: x => x.subject_id,
                    principalSchema: "academic",
                    principalTable: "subjects",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "topic_translations",
            schema: "academic",
            columns: table => new
            {
                topic_id = table.Column<Guid>(type: "uuid", nullable: false),
                language = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_topic_translations", x => new { x.topic_id, x.language });
                table.ForeignKey(
                    name: "fk_topic_translations_topics_topic_id",
                    column: x => x.topic_id,
                    principalSchema: "academic",
                    principalTable: "topics",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("ix_subjects_code", "subjects", "code", "academic", unique: true);
        migrationBuilder.CreateIndex("ix_subjects_status_display_order", "subjects", new[] { "status", "display_order" }, "academic");
        migrationBuilder.CreateIndex("ix_topics_subject_id_code", "topics", new[] { "subject_id", "code" }, "academic", unique: true);
        migrationBuilder.CreateIndex("ix_topics_subject_id_status_display_order", "topics", new[] { "subject_id", "status", "display_order" }, "academic");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("subject_translations", "academic");
        migrationBuilder.DropTable("topic_translations", "academic");
        migrationBuilder.DropTable("topics", "academic");
        migrationBuilder.DropTable("subjects", "academic");
    }
}
