using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATG.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProcurementWorkflowRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "procurement_workflow_role_assignments",
                columns: table => new
                {
                    RoleKey = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ManagerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    EngineerDepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_procurement_workflow_role_assignments", x => x.RoleKey);
                    table.ForeignKey(
                        name: "FK_procurement_workflow_role_assignments_departments_EngineerD~",
                        column: x => x.EngineerDepartmentId,
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_procurement_workflow_role_assignments_users_ManagerUserId",
                        column: x => x.ManagerUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_procurement_workflow_role_assignments_EngineerDepartmentId",
                table: "procurement_workflow_role_assignments",
                column: "EngineerDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_procurement_workflow_role_assignments_ManagerUserId",
                table: "procurement_workflow_role_assignments",
                column: "ManagerUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "procurement_workflow_role_assignments");
        }
    }
}
