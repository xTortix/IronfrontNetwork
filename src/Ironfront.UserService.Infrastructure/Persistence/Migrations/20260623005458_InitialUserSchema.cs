using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ironfront.UserService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialUserSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_profiles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_profiles", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "user_decks",
                columns: table => new
                {
                    DeckId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Nation = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_decks", x => x.DeckId);
                    table.ForeignKey(
                        name: "FK_user_decks_user_profiles_UserId",
                        column: x => x.UserId,
                        principalTable: "user_profiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_vehicles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UnlockedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_vehicles", x => new { x.UserId, x.VehicleId });
                    table.ForeignKey(
                        name: "FK_user_vehicles_user_profiles_UserId",
                        column: x => x.UserId,
                        principalTable: "user_profiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_deck_slots",
                columns: table => new
                {
                    DeckId = table.Column<Guid>(type: "uuid", nullable: false),
                    SlotIndex = table.Column<int>(type: "integer", nullable: false),
                    VehicleId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AddedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_deck_slots", x => new { x.DeckId, x.SlotIndex });
                    table.ForeignKey(
                        name: "FK_user_deck_slots_user_decks_DeckId",
                        column: x => x.DeckId,
                        principalTable: "user_decks",
                        principalColumn: "DeckId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_deck_slots_DeckId_VehicleId",
                table: "user_deck_slots",
                columns: new[] { "DeckId", "VehicleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_deck_slots_VehicleId",
                table: "user_deck_slots",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_user_decks_UserId",
                table: "user_decks",
                column: "UserId",
                unique: true,
                filter: "\"IsActive\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_user_vehicles_VehicleId",
                table: "user_vehicles",
                column: "VehicleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_deck_slots");

            migrationBuilder.DropTable(
                name: "user_vehicles");

            migrationBuilder.DropTable(
                name: "user_decks");

            migrationBuilder.DropTable(
                name: "user_profiles");
        }
    }
}
