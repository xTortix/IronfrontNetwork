using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ironfront.Matchmaking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBattleQueueTeamsAndRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ModeRevision",
                table: "battle_matches",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "RulesSnapshotJson",
                table: "battle_matches",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<byte>(
                name: "TeamId",
                table: "battle_match_players",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<int>(
                name: "TeamSlotIndex",
                table: "battle_match_players",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            /*
             * Vorhandene Development-Testdaten besitzen noch keine Teamstruktur.
             *
             * PlayerSlotIndex war bereits je Match eindeutig. Deshalb übernehmen
             * wir ihn als TeamSlotIndex für das neutrale Legacy-Team 0, bevor der
             * neue eindeutige Team-Slot-Index angelegt wird.
             */
            migrationBuilder.Sql(
                """
                UPDATE battle_match_players
                SET "TeamSlotIndex" = "PlayerSlotIndex"
                WHERE "TeamId" = 0;;
                """);
            
            migrationBuilder.CreateTable(
                name: "battle_queue_entries",
                columns: table => new
                {
                    QueueEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeckId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModeId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ModeRevision = table.Column<int>(type: "integer", nullable: false),
                    InitialVehicleId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    VehicleBattleRatingTenths = table.Column<byte>(type: "smallint", nullable: false),
                    VehicleClass = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    QueuedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MatchedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_battle_queue_entries", x => x.QueueEntryId);
                    table.ForeignKey(
                        name: "FK_battle_queue_entries_battle_matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "battle_matches",
                        principalColumn: "MatchId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_battle_matches_ModeId_ModeRevision",
                table: "battle_matches",
                columns: new[] { "ModeId", "ModeRevision" });

            migrationBuilder.CreateIndex(
                name: "IX_battle_match_players_MatchId_TeamId_TeamSlotIndex",
                table: "battle_match_players",
                columns: new[] { "MatchId", "TeamId", "TeamSlotIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_battle_queue_entries_MatchId",
                table: "battle_queue_entries",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_battle_queue_entries_ModeId_ModeRevision_Status_VehicleBatt~",
                table: "battle_queue_entries",
                columns: new[] { "ModeId", "ModeRevision", "Status", "VehicleBattleRatingTenths", "QueuedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_battle_queue_entries_UserId",
                table: "battle_queue_entries",
                column: "UserId",
                unique: true,
                filter: "\"Status\" IN ('Queued', 'Matched')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "battle_queue_entries");

            migrationBuilder.DropIndex(
                name: "IX_battle_matches_ModeId_ModeRevision",
                table: "battle_matches");

            migrationBuilder.DropIndex(
                name: "IX_battle_match_players_MatchId_TeamId_TeamSlotIndex",
                table: "battle_match_players");

            migrationBuilder.DropColumn(
                name: "ModeRevision",
                table: "battle_matches");

            migrationBuilder.DropColumn(
                name: "RulesSnapshotJson",
                table: "battle_matches");

            migrationBuilder.DropColumn(
                name: "TeamId",
                table: "battle_match_players");

            migrationBuilder.DropColumn(
                name: "TeamSlotIndex",
                table: "battle_match_players");
        }
    }
}
