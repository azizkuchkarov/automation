using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATG.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddItAutomationAssets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "it_assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    NameRu = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    NameEn = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Term = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BudgetCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BudgetAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ResponsibleUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartsAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ContractNumber = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ContractDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PlanYear = table.Column<int>(type: "integer", nullable: false),
                    LastExpiryWarningAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_it_assets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_it_assets_users_ResponsibleUserId",
                        column: x => x.ResponsibleUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "it_automation_role_assignments",
                columns: table => new
                {
                    Category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ResponsibleUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_it_automation_role_assignments", x => x.Category);
                    table.ForeignKey(
                        name: "FK_it_automation_role_assignments_users_ResponsibleUserId",
                        column: x => x.ResponsibleUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_it_assets_Category",
                table: "it_assets",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_it_assets_ExpiresAt",
                table: "it_assets",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_it_assets_PlanYear",
                table: "it_assets",
                column: "PlanYear");

            migrationBuilder.CreateIndex(
                name: "IX_it_assets_ResponsibleUserId",
                table: "it_assets",
                column: "ResponsibleUserId");

            migrationBuilder.CreateIndex(
                name: "IX_it_automation_role_assignments_ResponsibleUserId",
                table: "it_automation_role_assignments",
                column: "ResponsibleUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "it_assets");

            migrationBuilder.DropTable(
                name: "it_automation_role_assignments");
        }
    }
}
