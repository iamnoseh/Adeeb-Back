using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adeeb.Modules.Progression.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialProgression : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "progression");

            migrationBuilder.CreateTable(
                name: "league_definitions",
                schema: "progression",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name_tg = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name_ru = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    avatar_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    min_lifetime_xp_units = table.Column<long>(type: "bigint", nullable: false),
                    max_lifetime_xp_units = table.Column<long>(type: "bigint", nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    configuration_version = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_league_definitions", x => x.id);
                    table.CheckConstraint("ck_progression_leagues_range", "min_lifetime_xp_units >= 0 AND (max_lifetime_xp_units IS NULL OR max_lifetime_xp_units > min_lifetime_xp_units)");
                });

            migrationBuilder.CreateTable(
                name: "league_movement_results",
                schema: "progression",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_league_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_league_id = table.Column<Guid>(type: "uuid", nullable: false),
                    final_rank = table.Column<int>(type: "integer", nullable: false),
                    outcome = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_league_movement_results", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "league_seasons",
                schema: "progression",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    starts_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ends_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    auto_start_next = table.Column<bool>(type: "boolean", nullable: false),
                    configuration_version = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    closed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_league_seasons", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "league_memberships",
                schema: "progression",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    league_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    initial_lifetime_xp_units = table.Column<long>(type: "bigint", nullable: false),
                    season_score_units = table.Column<long>(type: "bigint", nullable: false),
                    joined_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_score_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    final_rank = table.Column<int>(type: "integer", nullable: true),
                    outcome = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_league_memberships", x => x.id);
                    table.ForeignKey(
                        name: "FK_league_memberships_league_definitions_league_id",
                        column: x => x.league_id,
                        principalSchema: "progression",
                        principalTable: "league_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_league_memberships_league_seasons_season_id",
                        column: x => x.season_id,
                        principalSchema: "progression",
                        principalTable: "league_seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "league_score_events",
                schema: "progression",
                columns: table => new
                {
                    ledger_entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    membership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount_units = table.Column<int>(type: "integer", nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_progression_score_events_ledger", x => x.ledger_entry_id);
                    table.ForeignKey(
                        name: "FK_league_score_events_league_memberships_membership_id",
                        column: x => x.membership_id,
                        principalSchema: "progression",
                        principalTable: "league_memberships",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_progression_leagues_order_active",
                schema: "progression",
                table: "league_definitions",
                column: "display_order",
                unique: true,
                filter: "status = 1");

            migrationBuilder.CreateIndex(
                name: "IX_league_memberships_league_id",
                schema: "progression",
                table: "league_memberships",
                column: "league_id");

            migrationBuilder.CreateIndex(
                name: "ix_progression_memberships_leaderboard",
                schema: "progression",
                table: "league_memberships",
                columns: new[] { "season_id", "league_id", "season_score_units", "last_score_at_utc", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ux_progression_memberships_season_user",
                schema: "progression",
                table: "league_memberships",
                columns: new[] { "season_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_progression_movements_season_user",
                schema: "progression",
                table: "league_movement_results",
                columns: new[] { "season_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_league_score_events_membership_id",
                schema: "progression",
                table: "league_score_events",
                column: "membership_id");

            migrationBuilder.CreateIndex(
                name: "IX_league_seasons_number",
                schema: "progression",
                table: "league_seasons",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_progression_seasons_active",
                schema: "progression",
                table: "league_seasons",
                column: "status",
                unique: true,
                filter: "status = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "league_movement_results",
                schema: "progression");

            migrationBuilder.DropTable(
                name: "league_score_events",
                schema: "progression");

            migrationBuilder.DropTable(
                name: "league_memberships",
                schema: "progression");

            migrationBuilder.DropTable(
                name: "league_definitions",
                schema: "progression");

            migrationBuilder.DropTable(
                name: "league_seasons",
                schema: "progression");
        }
    }
}
