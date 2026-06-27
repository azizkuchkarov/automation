using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATG.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketingWorkflowSteps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MarketingActiveBranch",
                table: "procurement_request_details",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MarketingBranchStartedAt",
                table: "procurement_request_details",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MarketingCurrentStep",
                table: "procurement_request_details",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql(
                """
                UPDATE procurement_request_details
                SET "MarketingCurrentStep" = CASE
                    WHEN "MarketingSubPhase" = 'Completed' THEN 11
                    WHEN "MarketingSubPhase" = 'InProgress' AND "MarketingSpecialistId" IS NOT NULL THEN 2
                    WHEN "MarketingSubPhase" = 'InProgress' THEN 2
                    ELSE 1
                END
                WHERE "Phase" = 'Marketing';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MarketingActiveBranch",
                table: "procurement_request_details");

            migrationBuilder.DropColumn(
                name: "MarketingBranchStartedAt",
                table: "procurement_request_details");

            migrationBuilder.DropColumn(
                name: "MarketingCurrentStep",
                table: "procurement_request_details");
        }
    }
}
