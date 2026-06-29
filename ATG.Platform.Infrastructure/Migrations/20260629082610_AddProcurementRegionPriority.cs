using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATG.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProcurementRegionPriority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "procurement_request_details",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "procurement_request_details",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RegionLabelEn",
                table: "procurement_request_details",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegionLabelRu",
                table: "procurement_request_details",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Priority",
                table: "procurement_request_details");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "procurement_request_details");

            migrationBuilder.DropColumn(
                name: "RegionLabelEn",
                table: "procurement_request_details");

            migrationBuilder.DropColumn(
                name: "RegionLabelRu",
                table: "procurement_request_details");
        }
    }
}
