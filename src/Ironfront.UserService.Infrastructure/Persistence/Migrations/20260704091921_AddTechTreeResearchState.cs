using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ironfront.UserService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTechTreeResearchState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_research_nodes",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    NodeId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ResearchPointsApplied = table.Column<long>(type: "bigint", nullable: false),
                    ResearchStartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResearchedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PurchasedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_research_nodes", x => new { x.UserId, x.NodeId });
                    table.ForeignKey(
                        name: "FK_user_research_nodes_user_profiles_UserId",
                        column: x => x.UserId,
                        principalTable: "user_profiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_tech_tree_states",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TechTreeId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SelectedNodeId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_tech_tree_states", x => new { x.UserId, x.TechTreeId });
                    table.ForeignKey(
                        name: "FK_user_tech_tree_states_user_profiles_UserId",
                        column: x => x.UserId,
                        principalTable: "user_profiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_research_nodes_NodeId",
                table: "user_research_nodes",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_user_tech_tree_states_TechTreeId",
                table: "user_tech_tree_states",
                column: "TechTreeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_research_nodes");

            migrationBuilder.DropTable(
                name: "user_tech_tree_states");
        }
    }
}
