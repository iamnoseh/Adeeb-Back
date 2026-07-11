using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adeeb.Modules.Students.Infrastructure.Persistence.Migrations;

[DbContext(typeof(StudentsDbContext))]
[Migration("20260711120000_InitialStudents")]
public partial class InitialStudents : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "students");

        migrationBuilder.CreateTable(
            name: "students",
            schema: "students",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                identity_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                onboarding_state = table.Column<int>(type: "integer", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_students", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "student_profiles",
            schema: "students",
            columns: table => new
            {
                student_id = table.Column<Guid>(type: "uuid", nullable: false),
                display_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                avatar_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                region = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                city = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                school_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                grade = table.Column<short>(type: "smallint", nullable: true),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_student_profiles", x => x.student_id);
                table.ForeignKey(
                    name: "FK_student_profiles_students_student_id",
                    column: x => x.student_id,
                    principalSchema: "students",
                    principalTable: "students",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_students_identity_user_id",
            schema: "students",
            table: "students",
            column: "identity_user_id",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "student_profiles", schema: "students");
        migrationBuilder.DropTable(name: "students", schema: "students");
    }
}
