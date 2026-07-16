using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATG.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_work_tasks_OrganizationId",
                table: "work_tasks");

            migrationBuilder.CreateIndex(
                name: "IX_work_tasks_OrganizationId_Status",
                table: "work_tasks",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_work_tasks_Status",
                table: "work_tasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_procurement_request_details_Phase",
                table: "procurement_request_details",
                column: "Phase");

            migrationBuilder.CreateIndex(
                name: "IX_documents_Status",
                table: "documents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_documents_Type",
                table: "documents",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_work_tasks_OrganizationId_Status",
                table: "work_tasks");

            migrationBuilder.DropIndex(
                name: "IX_work_tasks_Status",
                table: "work_tasks");

            migrationBuilder.DropIndex(
                name: "IX_procurement_request_details_Phase",
                table: "procurement_request_details");

            migrationBuilder.DropIndex(
                name: "IX_documents_Status",
                table: "documents");

            migrationBuilder.DropIndex(
                name: "IX_documents_Type",
                table: "documents");

            migrationBuilder.CreateIndex(
                name: "IX_work_tasks_OrganizationId",
                table: "work_tasks",
                column: "OrganizationId");
        }
    }
}
