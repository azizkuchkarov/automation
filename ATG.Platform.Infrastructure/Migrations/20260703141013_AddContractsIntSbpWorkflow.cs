using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATG.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContractsIntSbpWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ContractsIntCompletedAt",
                table: "procurement_request_details",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ContractsIntCurrentStep",
                table: "procurement_request_details",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ContractsIntVariant",
                table: "procurement_request_details",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ContractsIntVariantSelectedAt",
                table: "procurement_request_details",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContractsIntCompletedAt",
                table: "procurement_request_details");

            migrationBuilder.DropColumn(
                name: "ContractsIntCurrentStep",
                table: "procurement_request_details");

            migrationBuilder.DropColumn(
                name: "ContractsIntVariant",
                table: "procurement_request_details");

            migrationBuilder.DropColumn(
                name: "ContractsIntVariantSelectedAt",
                table: "procurement_request_details");
        }
    }
}
