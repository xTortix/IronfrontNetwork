using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ironfront.Matchmaking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBattleMatchPlayersAndJoinTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "battle_match_players",
                columns: table => new
                {
                    MatchPlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    InitialVehicleId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PlayerSlotIndex = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConnectedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DisconnectedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LeftAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_battle_match_players", x => x.MatchPlayerId);
                    table.UniqueConstraint("AK_battle_match_players_MatchId_MatchPlayerId", x => new { x.MatchId, x.MatchPlayerId });
                    table.ForeignKey(
                        name: "FK_battle_match_players_battle_matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "battle_matches",
                        principalColumn: "MatchId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "battle_join_tickets",
                columns: table => new
                {
                    TicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchPlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    BattleServerInstanceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SecretHash = table.Column<byte[]>(type: "bytea", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IssuedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsumedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_battle_join_tickets", x => x.TicketId);
                    table.ForeignKey(
                        name: "FK_battle_join_tickets_battle_match_players_MatchId_MatchPlaye~",
                        columns: x => new { x.MatchId, x.MatchPlayerId },
                        principalTable: "battle_match_players",
                        principalColumns: new[] { "MatchId", "MatchPlayerId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_battle_join_tickets_battle_matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "battle_matches",
                        principalColumn: "MatchId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_battle_join_tickets_battle_server_slots_BattleServerInstanc~",
                        column: x => x.BattleServerInstanceId,
                        principalTable: "battle_server_slots",
                        principalColumn: "ServerInstanceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_battle_join_tickets_BattleServerInstanceId_Status_ExpiresAt~",
                table: "battle_join_tickets",
                columns: new[] { "BattleServerInstanceId", "Status", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_battle_join_tickets_MatchId_MatchPlayerId",
                table: "battle_join_tickets",
                columns: new[] { "MatchId", "MatchPlayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_battle_match_players_MatchId_PlayerSlotIndex",
                table: "battle_match_players",
                columns: new[] { "MatchId", "PlayerSlotIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_battle_match_players_MatchId_Status_CreatedAtUtc",
                table: "battle_match_players",
                columns: new[] { "MatchId", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_battle_match_players_MatchId_UserId",
                table: "battle_match_players",
                columns: new[] { "MatchId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "battle_join_tickets");

            migrationBuilder.DropTable(
                name: "battle_match_players");
        }
    }
}
