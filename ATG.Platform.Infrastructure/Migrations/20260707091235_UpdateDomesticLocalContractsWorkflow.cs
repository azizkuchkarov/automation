using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATG.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDomesticLocalContractsWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ContractsDomActualDeliveryDate",
                table: "procurement_request_details",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ContractsDomDeliveryDueDate",
                table: "procurement_request_details",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ContractsDomLastTerminationAt",
                table: "procurement_request_details",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ContractsDomPriceRequestDate",
                table: "procurement_request_details",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ContractsDomPriceResponseDueDate",
                table: "procurement_request_details",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContractsDomActualDeliveryDate",
                table: "procurement_request_details");

            migrationBuilder.DropColumn(
                name: "ContractsDomDeliveryDueDate",
                table: "procurement_request_details");

            migrationBuilder.DropColumn(
                name: "ContractsDomLastTerminationAt",
                table: "procurement_request_details");

            migrationBuilder.DropColumn(
                name: "ContractsDomPriceRequestDate",
                table: "procurement_request_details");

            migrationBuilder.DropColumn(
                name: "ContractsDomPriceResponseDueDate",
                table: "procurement_request_details");
        }
    }
}
