using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adeeb.Modules.Students.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentEducationCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
            migrationBuilder.CreateTable(
                name: "academic_year_rollovers",
                schema: "students",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    academic_year_start = table.Column<int>(type: "integer", nullable: false),
                    academic_year_end = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    preview_created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    approved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    executed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    executed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    promoted_count = table.Column<int>(type: "integer", nullable: false),
                    graduated_count = table.Column<int>(type: "integer", nullable: false),
                    skipped_count = table.Column<int>(type: "integer", nullable: false),
                    conflict_count = table.Column<int>(type: "integer", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_academic_year_rollovers", x => x.id);
                    table.CheckConstraint("CK_academic_year_rollovers_year", "academic_year_end = academic_year_start + 1");
                });

            migrationBuilder.CreateTable(
                name: "regions",
                schema: "students",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<int>(type: "integer", nullable: false),
                    name_tg = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    name_ru = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    normalized_name_tg = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    normalized_name_ru = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    full_path_tg = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    full_path_ru = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    path_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    depth = table.Column<int>(type: "integer", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_regions", x => x.id);
                    table.CheckConstraint("CK_regions_depth_non_negative", "depth >= 0");
                    table.ForeignKey(
                        name: "FK_regions_regions_parent_id",
                        column: x => x.parent_id,
                        principalSchema: "students",
                        principalTable: "regions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "student_education_audit_logs",
                schema: "students",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    resource_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    resource_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: true),
                    old_values_json = table.Column<string>(type: "text", nullable: true),
                    new_values_json = table.Column<string>(type: "text", nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_education_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "academic_year_rollover_items",
                schema: "students",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rollover_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_version = table.Column<long>(type: "bigint", nullable: false),
                    source_grade = table.Column<short>(type: "smallint", nullable: true),
                    action = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_academic_year_rollover_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_academic_year_rollover_items_academic_year_rollovers_rollov~",
                        column: x => x.rollover_id,
                        principalSchema: "students",
                        principalTable: "academic_year_rollovers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "schools",
                schema: "students",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    region_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name_tg = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    name_ru = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    short_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    number = table.Column<int>(type: "integer", nullable: true),
                    type = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    search_text = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    address_text = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    verified_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    verified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    archived_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    archived_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    merged_into_school_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schools", x => x.id);
                    table.CheckConstraint("CK_schools_number_positive", "number IS NULL OR number > 0");
                    table.ForeignKey(
                        name: "FK_schools_regions_region_id",
                        column: x => x.region_id,
                        principalSchema: "students",
                        principalTable: "regions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_schools_schools_merged_into_school_id",
                        column: x => x.merged_into_school_id,
                        principalSchema: "students",
                        principalTable: "schools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "school_suggestions",
                schema: "students",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    submitted_by_student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    suggested_name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    suggested_number = table.Column<int>(type: "integer", nullable: true),
                    region_id = table.Column<Guid>(type: "uuid", nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    address_text = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    approved_school_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reviewed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reviewed_by_admin_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_school_suggestions", x => x.id);
                    table.ForeignKey(
                        name: "FK_school_suggestions_regions_region_id",
                        column: x => x.region_id,
                        principalSchema: "students",
                        principalTable: "regions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_school_suggestions_schools_approved_school_id",
                        column: x => x.approved_school_id,
                        principalSchema: "students",
                        principalTable: "schools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_school_suggestions_students_submitted_by_student_id",
                        column: x => x.submitted_by_student_id,
                        principalSchema: "students",
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "student_school_enrollments",
                schema: "students",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    region_id = table.Column<Guid>(type: "uuid", nullable: false),
                    grade = table.Column<short>(type: "smallint", nullable: false),
                    academic_year_start = table.Column<int>(type: "integer", nullable: false),
                    academic_year_end = table.Column<int>(type: "integer", nullable: false),
                    is_current = table.Column<bool>(type: "boolean", nullable: false),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ended_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    change_reason = table.Column<string>(type: "character varying(280)", maxLength: 280, nullable: true),
                    source = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_school_enrollments", x => x.id);
                    table.CheckConstraint("CK_student_school_enrollments_grade", "grade >= 1 AND grade <= 11");
                    table.CheckConstraint("CK_student_school_enrollments_year", "academic_year_end = academic_year_start + 1");
                    table.ForeignKey(
                        name: "FK_student_school_enrollments_regions_region_id",
                        column: x => x.region_id,
                        principalSchema: "students",
                        principalTable: "regions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_student_school_enrollments_schools_school_id",
                        column: x => x.school_id,
                        principalSchema: "students",
                        principalTable: "schools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_student_school_enrollments_students_student_id",
                        column: x => x.student_id,
                        principalSchema: "students",
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "student_education_profiles",
                schema: "students",
                columns: table => new
                {
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    residence_region_id = table.Column<Guid>(type: "uuid", nullable: true),
                    school_id = table.Column<Guid>(type: "uuid", nullable: true),
                    pending_school_suggestion_id = table.Column<Guid>(type: "uuid", nullable: true),
                    current_grade = table.Column<short>(type: "smallint", nullable: true),
                    academic_year_start = table.Column<int>(type: "integer", nullable: true),
                    academic_year_end = table.Column<int>(type: "integer", nullable: true),
                    expected_graduation_year = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    address_text = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_education_profiles", x => x.student_id);
                    table.CheckConstraint("CK_student_education_profiles_grade", "current_grade IS NULL OR (current_grade >= 1 AND current_grade <= 11)");
                    table.CheckConstraint("CK_student_education_profiles_year", "academic_year_start IS NULL OR academic_year_end = academic_year_start + 1");
                    table.ForeignKey(
                        name: "FK_student_education_profiles_regions_residence_region_id",
                        column: x => x.residence_region_id,
                        principalSchema: "students",
                        principalTable: "regions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_student_education_profiles_school_suggestions_pending_schoo~",
                        column: x => x.pending_school_suggestion_id,
                        principalSchema: "students",
                        principalTable: "school_suggestions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_student_education_profiles_schools_school_id",
                        column: x => x.school_id,
                        principalSchema: "students",
                        principalTable: "schools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_student_education_profiles_students_student_id",
                        column: x => x.student_id,
                        principalSchema: "students",
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_academic_year_rollover_items_rollover_id_student_id",
                schema: "students",
                table: "academic_year_rollover_items",
                columns: new[] { "rollover_id", "student_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_academic_year_rollovers_idempotency",
                schema: "students",
                table: "academic_year_rollovers",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_academic_year_rollovers_year",
                schema: "students",
                table: "academic_year_rollovers",
                columns: new[] { "academic_year_start", "academic_year_end" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_regions_path_ids",
                schema: "students",
                table: "regions",
                column: "path_ids")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "UX_regions_parent_type_name",
                schema: "students",
                table: "regions",
                columns: new[] { "parent_id", "type", "normalized_name_ru" },
                unique: true,
                filter: "parent_id IS NOT NULL AND is_active = true");

            migrationBuilder.CreateIndex(
                name: "UX_regions_root_type_name",
                schema: "students",
                table: "regions",
                columns: new[] { "type", "normalized_name_ru" },
                unique: true,
                filter: "parent_id IS NULL AND is_active = true");

            migrationBuilder.CreateIndex(
                name: "IX_school_suggestions_approved_school_id",
                schema: "students",
                table: "school_suggestions",
                column: "approved_school_id");

            migrationBuilder.CreateIndex(
                name: "IX_school_suggestions_region_id",
                schema: "students",
                table: "school_suggestions",
                column: "region_id");

            migrationBuilder.CreateIndex(
                name: "UX_school_suggestions_pending_student_name",
                schema: "students",
                table: "school_suggestions",
                columns: new[] { "submitted_by_student_id", "region_id", "normalized_name" },
                unique: true,
                filter: "status = 0");

            migrationBuilder.CreateIndex(
                name: "IX_schools_merged_into_school_id",
                schema: "students",
                table: "schools",
                column: "merged_into_school_id");

            migrationBuilder.CreateIndex(
                name: "IX_schools_region_id_status_number",
                schema: "students",
                table: "schools",
                columns: new[] { "region_id", "status", "number" });

            migrationBuilder.CreateIndex(
                name: "UX_schools_region_name_type_live",
                schema: "students",
                table: "schools",
                columns: new[] { "region_id", "normalized_name", "type" },
                unique: true,
                filter: "number IS NULL AND status IN (0, 1, 2)");

            migrationBuilder.CreateIndex(
                name: "UX_schools_region_number_type_live",
                schema: "students",
                table: "schools",
                columns: new[] { "region_id", "number", "type" },
                unique: true,
                filter: "number IS NOT NULL AND status IN (0, 1, 2)");

            migrationBuilder.Sql("CREATE INDEX \"IX_schools_search_text_trgm\" ON students.schools USING gin (search_text gin_trgm_ops);");

            migrationBuilder.CreateIndex(
                name: "IX_student_education_audit_logs_resource_type_resource_id_crea~",
                schema: "students",
                table: "student_education_audit_logs",
                columns: new[] { "resource_type", "resource_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_student_education_audit_logs_student_id_created_at_utc",
                schema: "students",
                table: "student_education_audit_logs",
                columns: new[] { "student_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_student_education_profiles_pending_school_suggestion_id",
                schema: "students",
                table: "student_education_profiles",
                column: "pending_school_suggestion_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_education_profiles_residence_region_id",
                schema: "students",
                table: "student_education_profiles",
                column: "residence_region_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_education_profiles_school_id",
                schema: "students",
                table: "student_education_profiles",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "UX_student_education_profiles_student",
                schema: "students",
                table: "student_education_profiles",
                column: "student_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_student_school_enrollments_grade_academic_year_start_academ~",
                schema: "students",
                table: "student_school_enrollments",
                columns: new[] { "grade", "academic_year_start", "academic_year_end" });

            migrationBuilder.CreateIndex(
                name: "IX_student_school_enrollments_region_id_academic_year_start_ac~",
                schema: "students",
                table: "student_school_enrollments",
                columns: new[] { "region_id", "academic_year_start", "academic_year_end" });

            migrationBuilder.CreateIndex(
                name: "IX_student_school_enrollments_school_id_academic_year_start_ac~",
                schema: "students",
                table: "student_school_enrollments",
                columns: new[] { "school_id", "academic_year_start", "academic_year_end" });

            migrationBuilder.CreateIndex(
                name: "IX_student_school_enrollments_student_id_academic_year_start_a~",
                schema: "students",
                table: "student_school_enrollments",
                columns: new[] { "student_id", "academic_year_start", "academic_year_end" });

            migrationBuilder.CreateIndex(
                name: "UX_student_school_enrollments_current_student",
                schema: "students",
                table: "student_school_enrollments",
                column: "student_id",
                unique: true,
                filter: "is_current = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "academic_year_rollover_items",
                schema: "students");

            migrationBuilder.DropTable(
                name: "student_education_audit_logs",
                schema: "students");

            migrationBuilder.DropTable(
                name: "student_education_profiles",
                schema: "students");

            migrationBuilder.DropTable(
                name: "student_school_enrollments",
                schema: "students");

            migrationBuilder.DropTable(
                name: "academic_year_rollovers",
                schema: "students");

            migrationBuilder.DropTable(
                name: "school_suggestions",
                schema: "students");

            migrationBuilder.DropTable(
                name: "schools",
                schema: "students");

            migrationBuilder.DropTable(
                name: "regions",
                schema: "students");
        }
    }
}
