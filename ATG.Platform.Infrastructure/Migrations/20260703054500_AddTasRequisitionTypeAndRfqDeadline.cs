using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATG.Platform.Infrastructure.Migrations
{
    public partial class AddTasRequisitionTypeAndRfqDeadline : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "RfqCommercialProposalDeadline",
                table: "marketing_records",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TasRequisitionType",
                table: "procurement_request_details",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RfqCommercialProposalDeadline",
                table: "marketing_records");

            migrationBuilder.DropColumn(
                name: "TasRequisitionType",
                table: "procurement_request_details");
        }
    }
}
