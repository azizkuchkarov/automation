using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATG.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSbpStepArtifactsAndPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ContractsIntContractRegisteredAt",
                table: "procurement_request_details",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContractsIntContractRegistrationNumber",
                table: "procurement_request_details",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ContractsIntSecretariatPending",
                table: "procurement_request_details",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ContractsIntSecretariatUserId",
                table: "procurement_request_details",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentAcceptedAt",
                table: "procurement_request_details",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentAssignedAt",
                table: "procurement_request_details",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentSpecialistId",
                table: "procurement_request_details",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentSubPhase",
                table: "procurement_request_details",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentTaskId",
                table: "procurement_request_details",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "procurement_contracts_int_step_approvers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepNumber = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DecidedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_procurement_contracts_int_step_approvers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_procurement_contracts_int_step_approvers_procurement_reques~",
                        column: x => x.DocumentId,
                        principalTable: "procurement_request_details",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_procurement_contracts_int_step_approvers_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "procurement_contracts_int_step_files",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepNumber = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UploadedById = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_procurement_contracts_int_step_files", x => x.Id);
                    table.ForeignKey(
                        name: "FK_procurement_contracts_int_step_files_procurement_request_de~",
                        column: x => x.DocumentId,
                        principalTable: "procurement_request_details",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_procurement_contracts_int_step_files_users_UploadedById",
                        column: x => x.UploadedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_procurement_request_details_ContractsIntSecretariatUserId",
                table: "procurement_request_details",
                column: "ContractsIntSecretariatUserId");

            migrationBuilder.CreateIndex(
                name: "IX_procurement_request_details_PaymentSpecialistId",
                table: "procurement_request_details",
                column: "PaymentSpecialistId");

            migrationBuilder.CreateIndex(
                name: "IX_procurement_contracts_int_step_approvers_DocumentId_StepNum~",
                table: "procurement_contracts_int_step_approvers",
                columns: new[] { "DocumentId", "StepNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_procurement_contracts_int_step_approvers_UserId",
                table: "procurement_contracts_int_step_approvers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_procurement_contracts_int_step_files_DocumentId_StepNumber",
                table: "procurement_contracts_int_step_files",
                columns: new[] { "DocumentId", "StepNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_procurement_contracts_int_step_files_UploadedById",
                table: "procurement_contracts_int_step_files",
                column: "UploadedById");

            migrationBuilder.AddForeignKey(
                name: "FK_procurement_request_details_users_ContractsIntSecretariatUs~",
                table: "procurement_request_details",
                column: "ContractsIntSecretariatUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_procurement_request_details_users_PaymentSpecialistId",
                table: "procurement_request_details",
                column: "PaymentSpecialistId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_procurement_request_details_users_ContractsIntSecretariatUs~",
                table: "procurement_request_details");

            migrationBuilder.DropForeignKey(
                name: "FK_procurement_request_details_users_PaymentSpecialistId",
                table: "procurement_request_details");

            migrationBuilder.DropTable(
                name: "procurement_contracts_int_step_approvers");

            migrationBuilder.DropTable(
                name: "procurement_contracts_int_step_files");

            migrationBuilder.DropIndex(
                name: "IX_procurement_request_details_ContractsIntSecretariatUserId",
                table: "procurement_request_details");

            migrationBuilder.DropIndex(
                name: "IX_procurement_request_details_PaymentSpecialistId",
                table: "procurement_request_details");

            migrationBuilder.DropColumn(
                name: "ContractsIntContractRegisteredAt",
                table: "procurement_request_details");

            migrationBuilder.DropColumn(
                name: "ContractsIntContractRegistrationNumber",
                table: "procurement_request_details");

            migrationBuilder.DropColumn(
                name: "ContractsIntSecretariatPending",
                table: "procurement_request_details");

            migrationBuilder.DropColumn(
                name: "ContractsIntSecretariatUserId",
                table: "procurement_request_details");

            migrationBuilder.DropColumn(
                name: "PaymentAcceptedAt",
                table: "procurement_request_details");

            migrationBuilder.DropColumn(
                name: "PaymentAssignedAt",
                table: "procurement_request_details");

            migrationBuilder.DropColumn(
                name: "PaymentSpecialistId",
                table: "procurement_request_details");

            migrationBuilder.DropColumn(
                name: "PaymentSubPhase",
                table: "procurement_request_details");

            migrationBuilder.DropColumn(
                name: "PaymentTaskId",
                table: "procurement_request_details");
        }
    }
}
