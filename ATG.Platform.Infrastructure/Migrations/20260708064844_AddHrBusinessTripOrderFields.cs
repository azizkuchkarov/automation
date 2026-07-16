using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATG.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHrBusinessTripOrderFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OrderDocxStorageKey",
                table: "hr_business_trip_request_details",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OrderIssuedAt",
                table: "hr_business_trip_request_details",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrderNumber",
                table: "hr_business_trip_request_details",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderDocxStorageKey",
                table: "hr_business_trip_request_details");

            migrationBuilder.DropColumn(
                name: "OrderIssuedAt",
                table: "hr_business_trip_request_details");

            migrationBuilder.DropColumn(
                name: "OrderNumber",
                table: "hr_business_trip_request_details");
        }
    }
}
