using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATG.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProcurementContractsSubPhase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ContractsAcceptedAt",
                table: "procurement_request_details",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ContractsAssignedAt",
                table: "procurement_request_details",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ContractsSpecialistId",
                table: "procurement_request_details",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContractsSubPhase",
                table: "procurement_request_details",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_procurement_request_details_ContractsSpecialistId",
                table: "procurement_request_details",
                column: "ContractsSpecialistId");

            migrationBuilder.AddForeignKey(
                name: "FK_procurement_request_details_users_ContractsSpecialistId",
                table: "procurement_request_details",
                column: "ContractsSpecialistId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_procurement_request_details_users_ContractsSpecialistId",
                table: "procurement_request_details");

            migrationBuilder.DropIndex(
                name: "IX_procurement_request_details_ContractsSpecialistId",
                table: "procurement_request_details");

            migrationBuilder.DropColumn(
                name: "ContractsAcceptedAt",
                table: "procurement_request_details");

            migrationBuilder.DropColumn(
                name: "ContractsAssignedAt",
                table: "procurement_request_details");

            migrationBuilder.DropColumn(
                name: "ContractsSpecialistId",
                table: "procurement_request_details");

            migrationBuilder.DropColumn(
                name: "ContractsSubPhase",
                table: "procurement_request_details");
        }
    }
}
