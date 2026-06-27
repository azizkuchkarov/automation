using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATG.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProcurementRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "procurement_request_details",
                columns: table => new
                {
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Flow = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Phase = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CurrentStep = table.Column<int>(type: "integer", nullable: false),
                    InitiatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    InitiatorDepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    EamNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EamFormationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResponsibleTaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    MarketingTaskId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_procurement_request_details", x => x.DocumentId);
                    table.ForeignKey(
                        name: "FK_procurement_request_details_departments_InitiatorDepartment~",
                        column: x => x.InitiatorDepartmentId,
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_procurement_request_details_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_procurement_request_details_users_InitiatorId",
                        column: x => x.InitiatorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "procurement_request_approvers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    DecidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_procurement_request_approvers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_procurement_request_approvers_procurement_request_details_D~",
                        column: x => x.DocumentId,
                        principalTable: "procurement_request_details",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_procurement_request_approvers_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "procurement_request_attachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    UploadedById = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_procurement_request_attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_procurement_request_attachments_procurement_request_details~",
                        column: x => x.DocumentId,
                        principalTable: "procurement_request_details",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_procurement_request_attachments_users_UploadedById",
                        column: x => x.UploadedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_procurement_request_approvers_DocumentId",
                table: "procurement_request_approvers",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_procurement_request_approvers_UserId",
                table: "procurement_request_approvers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_procurement_request_attachments_DocumentId",
                table: "procurement_request_attachments",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_procurement_request_attachments_UploadedById",
                table: "procurement_request_attachments",
                column: "UploadedById");

            migrationBuilder.CreateIndex(
                name: "IX_procurement_request_details_InitiatorDepartmentId",
                table: "procurement_request_details",
                column: "InitiatorDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_procurement_request_details_InitiatorId",
                table: "procurement_request_details",
                column: "InitiatorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "procurement_request_approvers");

            migrationBuilder.DropTable(
                name: "procurement_request_attachments");

            migrationBuilder.DropTable(
                name: "procurement_request_details");
        }
    }
}
