using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATG.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHrBusinessTripEimzo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EimzoCompletedAt",
                table: "hr_business_trip_request_details",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PdfPresentationStorageKey",
                table: "hr_business_trip_request_details",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PdfSignedStorageKey",
                table: "hr_business_trip_request_details",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SigningPayloadHash",
                table: "hr_business_trip_request_details",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "hr_business_trip_signatures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SignerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ApproverRole = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Pkcs7Base64 = table.Column<string>(type: "text", nullable: false),
                    PayloadSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CertificateSerial = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SignerPinpp = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    SignerCn = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    SignerTin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    SignedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hr_business_trip_signatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_hr_business_trip_signatures_hr_business_trip_request_detail~",
                        column: x => x.DocumentId,
                        principalTable: "hr_business_trip_request_details",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_hr_business_trip_signatures_users_SignerUserId",
                        column: x => x.SignerUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_hr_business_trip_signatures_DocumentId",
                table: "hr_business_trip_signatures",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_hr_business_trip_signatures_SignerUserId",
                table: "hr_business_trip_signatures",
                column: "SignerUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "hr_business_trip_signatures");

            migrationBuilder.DropColumn(
                name: "EimzoCompletedAt",
                table: "hr_business_trip_request_details");

            migrationBuilder.DropColumn(
                name: "PdfPresentationStorageKey",
                table: "hr_business_trip_request_details");

            migrationBuilder.DropColumn(
                name: "PdfSignedStorageKey",
                table: "hr_business_trip_request_details");

            migrationBuilder.DropColumn(
                name: "SigningPayloadHash",
                table: "hr_business_trip_request_details");
        }
    }
}
