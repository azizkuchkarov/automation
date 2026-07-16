using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATG.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MarketingWorkflowV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InitiatorReviewComment",
                table: "marketing_offers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InitiatorReviewStatus",
                table: "marketing_offers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "InitiatorReviewedAt",
                table: "marketing_offers",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InitiatorReviewedById",
                table: "marketing_offers",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE procurement_request_details
                SET "MarketingCurrentStep" = CASE
                    WHEN "MarketingCurrentStep" <= 2 THEN "MarketingCurrentStep"
                    WHEN "MarketingCurrentStep" IN (3, 4, 5) THEN 3
                    WHEN "MarketingCurrentStep" = 6 THEN 4
                    WHEN "MarketingCurrentStep" = 7 THEN 6
                    WHEN "MarketingCurrentStep" = 8 THEN 7
                    WHEN "MarketingCurrentStep" = 9 THEN 8
                    ELSE "MarketingCurrentStep"
                END,
                "MarketingActiveBranch" = CASE
                    WHEN "MarketingActiveBranch" IN ('ResponseFollowUp', 'KpNegotiation') THEN NULL
                    ELSE "MarketingActiveBranch"
                END
                WHERE "Phase" = 'Marketing';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InitiatorReviewComment",
                table: "marketing_offers");

            migrationBuilder.DropColumn(
                name: "InitiatorReviewStatus",
                table: "marketing_offers");

            migrationBuilder.DropColumn(
                name: "InitiatorReviewedAt",
                table: "marketing_offers");

            migrationBuilder.DropColumn(
                name: "InitiatorReviewedById",
                table: "marketing_offers");
        }
    }
}
