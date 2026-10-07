using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ironfront.Matchmaking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBattleServerCommandInbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "battle_server_commands",
                columns: table => new
                {
                    CommandId = table.Column<Guid>(type: "uuid", nullable: false),
                    BattleServerInstanceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CommandType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    State = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeliveredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcknowledgedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_battle_server_commands", x => x.CommandId);
                    table.ForeignKey(
                        name: "FK_battle_server_commands_battle_server_slots_BattleServerInst~",
                        column: x => x.BattleServerInstanceId,
                        principalTable: "battle_server_slots",
                        principalColumn: "ServerInstanceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_battle_server_commands_BattleServerInstanceId_State_Created~",
                table: "battle_server_commands",
                columns: new[] { "BattleServerInstanceId", "State", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "battle_server_commands");
        }
    }
}
