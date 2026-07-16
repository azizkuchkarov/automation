using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATG.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHrBusinessTripWorkflows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "hr_business_trip_dept_workflows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TitleRu = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    TitleEn = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hr_business_trip_dept_workflows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_hr_business_trip_dept_workflows_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hr_business_trip_workflow_tiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                    TierKey = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TitleRu = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    TitleEn = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    MatchPriority = table.Column<int>(type: "integer", nullable: false),
                    CatchAllStaff = table.Column<bool>(type: "boolean", nullable: false),
                    PrependsSectionManager = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hr_business_trip_workflow_tiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_hr_business_trip_workflow_tiers_hr_business_trip_dept_workf~",
                        column: x => x.WorkflowId,
                        principalTable: "hr_business_trip_dept_workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hr_business_trip_workflow_initiators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TierId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hr_business_trip_workflow_initiators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_hr_business_trip_workflow_initiators_hr_business_trip_workf~",
                        column: x => x.TierId,
                        principalTable: "hr_business_trip_workflow_tiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_hr_business_trip_workflow_initiators_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "hr_business_trip_workflow_steps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TierId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    ApproverUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    LabelRu = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    LabelEn = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hr_business_trip_workflow_steps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_hr_business_trip_workflow_steps_hr_business_trip_workflow_t~",
                        column: x => x.TierId,
                        principalTable: "hr_business_trip_workflow_tiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_hr_business_trip_workflow_steps_users_ApproverUserId",
                        column: x => x.ApproverUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_hr_business_trip_dept_workflows_OrganizationId_DepartmentCo~",
                table: "hr_business_trip_dept_workflows",
                columns: new[] { "OrganizationId", "DepartmentCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_hr_business_trip_workflow_initiators_TierId_UserId",
                table: "hr_business_trip_workflow_initiators",
                columns: new[] { "TierId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_hr_business_trip_workflow_initiators_UserId",
                table: "hr_business_trip_workflow_initiators",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_hr_business_trip_workflow_steps_ApproverUserId",
                table: "hr_business_trip_workflow_steps",
                column: "ApproverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_hr_business_trip_workflow_steps_TierId_SortOrder",
                table: "hr_business_trip_workflow_steps",
                columns: new[] { "TierId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_hr_business_trip_workflow_tiers_WorkflowId_TierKey",
                table: "hr_business_trip_workflow_tiers",
                columns: new[] { "WorkflowId", "TierKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "hr_business_trip_workflow_initiators");

            migrationBuilder.DropTable(
                name: "hr_business_trip_workflow_steps");

            migrationBuilder.DropTable(
                name: "hr_business_trip_workflow_tiers");

            migrationBuilder.DropTable(
                name: "hr_business_trip_dept_workflows");
        }
    }
}
