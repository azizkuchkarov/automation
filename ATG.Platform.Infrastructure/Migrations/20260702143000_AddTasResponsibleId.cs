using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATG.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTasResponsibleId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TasResponsibleId",
                table: "procurement_request_details",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_procurement_request_details_TasResponsibleId",
                table: "procurement_request_details",
                column: "TasResponsibleId");

            migrationBuilder.AddForeignKey(
                name: "FK_procurement_request_details_users_TasResponsibleId",
                table: "procurement_request_details",
                column: "TasResponsibleId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.Sql("""
                UPDATE procurement_request_details d
                SET "TasResponsibleId" = t."AssigneeId"
                FROM work_tasks t
                WHERE d."ResponsibleTaskId" = t."Id"
                  AND d."Flow" = 'TechnicalAffairs'
                  AND d."TasResponsibleId" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_procurement_request_details_users_TasResponsibleId",
                table: "procurement_request_details");

            migrationBuilder.DropIndex(
                name: "IX_procurement_request_details_TasResponsibleId",
                table: "procurement_request_details");

            migrationBuilder.DropColumn(
                name: "TasResponsibleId",
                table: "procurement_request_details");
        }
    }
}
