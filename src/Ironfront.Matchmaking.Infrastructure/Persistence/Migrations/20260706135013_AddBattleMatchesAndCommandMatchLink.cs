using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ironfront.Matchmaking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBattleMatchesAndCommandMatchLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MatchId",
                table: "battle_server_commands",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "battle_matches",
                columns: table => new
                {
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    BattleServerInstanceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ModeId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MapId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpectedPlayerCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WaitingForPlayersAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FinishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_battle_matches", x => x.MatchId);
                    table.ForeignKey(
                        name: "FK_battle_matches_battle_server_slots_BattleServerInstanceId",
                        column: x => x.BattleServerInstanceId,
                        principalTable: "battle_server_slots",
                        principalColumn: "ServerInstanceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_battle_server_commands_MatchId",
                table: "battle_server_commands",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_battle_matches_BattleServerInstanceId_Status_CreatedAtUtc",
                table: "battle_matches",
                columns: new[] { "BattleServerInstanceId", "Status", "CreatedAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_battle_server_commands_battle_matches_MatchId",
                table: "battle_server_commands",
                column: "MatchId",
                principalTable: "battle_matches",
                principalColumn: "MatchId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_battle_server_commands_battle_matches_MatchId",
                table: "battle_server_commands");

            migrationBuilder.DropTable(
                name: "battle_matches");

            migrationBuilder.DropIndex(
                name: "IX_battle_server_commands_MatchId",
                table: "battle_server_commands");

            migrationBuilder.DropColumn(
                name: "MatchId",
                table: "battle_server_commands");
        }
    }
}
