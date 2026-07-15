using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adeeb.Modules.Mmt.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMmtClusterSubjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cluster_subjects",
                schema: "mmt",
                columns: table => new
                {
                    cluster_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cluster_subjects", x => new { x.cluster_id, x.subject_id });
                    table.ForeignKey(
                        name: "FK_cluster_subjects_clusters_cluster_id",
                        column: x => x.cluster_id,
                        principalSchema: "mmt",
                        principalTable: "clusters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mmt_cluster_subjects_subject_id",
                schema: "mmt",
                table: "cluster_subjects",
                column: "subject_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cluster_subjects",
                schema: "mmt");
        }
    }
}
