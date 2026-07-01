using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATG.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHrLeaveRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "hr_leave_request_details",
                columns: table => new
                {
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Phase = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Track = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    HrDepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodLabel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RequestDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    HrTaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    HrReviewCompletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hr_leave_request_details", x => x.DocumentId);
                    table.ForeignKey(
                        name: "FK_hr_leave_request_details_departments_HrDepartmentId",
                        column: x => x.HrDepartmentId,
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_hr_leave_request_details_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hr_leave_approvers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    ApprovalGroup = table.Column<int>(type: "integer", nullable: false),
                    DecidedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hr_leave_approvers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_hr_leave_approvers_hr_leave_request_details_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "hr_leave_request_details",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_hr_leave_approvers_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "hr_leave_request_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DateFrom = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DateTo = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DaysCount = table.Column<int>(type: "integer", nullable: true),
                    NoteRu = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    NoteEn = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hr_leave_request_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_hr_leave_request_items_hr_leave_request_details_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "hr_leave_request_details",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_hr_leave_approvers_DocumentId",
                table: "hr_leave_approvers",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_hr_leave_approvers_UserId",
                table: "hr_leave_approvers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_hr_leave_request_details_HrDepartmentId",
                table: "hr_leave_request_details",
                column: "HrDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_hr_leave_request_items_DocumentId",
                table: "hr_leave_request_items",
                column: "DocumentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "hr_leave_approvers");

            migrationBuilder.DropTable(
                name: "hr_leave_request_items");

            migrationBuilder.DropTable(
                name: "hr_leave_request_details");
        }
    }
}
