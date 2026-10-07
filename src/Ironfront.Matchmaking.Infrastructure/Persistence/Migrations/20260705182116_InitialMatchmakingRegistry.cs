using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ironfront.Matchmaking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialMatchmakingRegistry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "battle_server_slots",
                columns: table => new
                {
                    ServerInstanceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    HostId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Region = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PublicHost = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PublicPort = table.Column<int>(type: "integer", nullable: false),
                    BuildVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CatalogVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PlayerCount = table.Column<int>(type: "integer", nullable: false),
                    MaxPlayers = table.Column<int>(type: "integer", nullable: false),
                    ActiveMatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    RegisteredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastHeartbeatUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_battle_server_slots", x => x.ServerInstanceId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_battle_server_slots_HostId",
                table: "battle_server_slots",
                column: "HostId");

            migrationBuilder.CreateIndex(
                name: "IX_battle_server_slots_Region_Status_LastHeartbeatUtc",
                table: "battle_server_slots",
                columns: new[] { "Region", "Status", "LastHeartbeatUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "battle_server_slots");
        }
    }
}
