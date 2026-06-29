using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATG.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketingPlanApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "MarketingPlanApprovalSubmittedAt",
                table: "procurement_request_details",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MarketingPlanRegisteredAt",
                table: "procurement_request_details",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MarketingPlanRegisteredById",
                table: "procurement_request_details",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MarketingPlanRegistrationNumber",
                table: "procurement_request_details",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "procurement_marketing_plan_approvers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    DecidedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Comment = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_procurement_marketing_plan_approvers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_procurement_marketing_plan_approvers_procurement_request_de~",
                        column: x => x.DocumentId,
                        principalTable: "procurement_request_details",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_procurement_marketing_plan_approvers_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_procurement_marketing_plan_approvers_DocumentId",
                table: "procurement_marketing_plan_approvers",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_procurement_marketing_plan_approvers_UserId",
                table: "procurement_marketing_plan_approvers",
                column: "UserId");

            migrationBuilder.Sql("""
                UPDATE procurement_request_details
                SET "MarketingCurrentStep" = 8
                WHERE "Phase" = 'Marketing' AND "MarketingCurrentStep" = 9;

                UPDATE procurement_request_details
                SET "MarketingCurrentStep" = 9
                WHERE "Phase" = 'Marketing' AND "MarketingCurrentStep" IN (10, 11);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "procurement_marketing_plan_approvers");

            migrationBuilder.DropColumn(
                name: "MarketingPlanApprovalSubmittedAt",
                table: "procurement_request_details");

            migrationBuilder.DropColumn(
                name: "MarketingPlanRegisteredAt",
                table: "procurement_request_details");

            migrationBuilder.DropColumn(
                name: "MarketingPlanRegisteredById",
                table: "procurement_request_details");

            migrationBuilder.DropColumn(
                name: "MarketingPlanRegistrationNumber",
                table: "procurement_request_details");
        }
    }
}
