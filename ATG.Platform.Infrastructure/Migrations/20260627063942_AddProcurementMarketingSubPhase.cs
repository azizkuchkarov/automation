using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATG.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProcurementMarketingSubPhase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "MarketingAcceptedAt",
                table: "procurement_request_details",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MarketingCompletedAt",
                table: "procurement_request_details",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MarketingSpecialistId",
                table: "procurement_request_details",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MarketingSubPhase",
                table: "procurement_request_details",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_procurement_request_details_MarketingSpecialistId",
                table: "procurement_request_details",
                column: "MarketingSpecialistId");

            migrationBuilder.AddForeignKey(
                name: "FK_procurement_request_details_users_MarketingSpecialistId",
                table: "procurement_request_details",
                column: "MarketingSpecialistId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_procurement_request_details_users_MarketingSpecialistId",
                table: "procurement_request_details");

            migrationBuilder.DropIndex(
                name: "IX_procurement_request_details_MarketingSpecialistId",
                table: "procurement_request_details");

            migrationBuilder.DropColumn(
                name: "MarketingAcceptedAt",
                table: "procurement_request_details");

            migrationBuilder.DropColumn(
                name: "MarketingCompletedAt",
                table: "procurement_request_details");

            migrationBuilder.DropColumn(
                name: "MarketingSpecialistId",
                table: "procurement_request_details");

            migrationBuilder.DropColumn(
                name: "MarketingSubPhase",
                table: "procurement_request_details");
        }
    }
}
