using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATG.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHrBusinessTripRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "hr_business_trip_request_details",
                columns: table => new
                {
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Phase = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RequestDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    PurposeRu = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    PurposeEn = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DateFrom = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DateTo = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DaysCount = table.Column<int>(type: "integer", nullable: false),
                    PlaceRu = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PlaceEn = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PdfStorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hr_business_trip_request_details", x => x.DocumentId);
                    table.ForeignKey(
                        name: "FK_hr_business_trip_request_details_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hr_business_trip_approvers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    DecidedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hr_business_trip_approvers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_hr_business_trip_approvers_hr_business_trip_request_details~",
                        column: x => x.DocumentId,
                        principalTable: "hr_business_trip_request_details",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_hr_business_trip_approvers_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "hr_business_trip_travelers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullNameRu = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FullNameEn = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PositionRu = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    PositionEn = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hr_business_trip_travelers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_hr_business_trip_travelers_hr_business_trip_request_details~",
                        column: x => x.DocumentId,
                        principalTable: "hr_business_trip_request_details",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_hr_business_trip_approvers_DocumentId",
                table: "hr_business_trip_approvers",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_hr_business_trip_approvers_UserId",
                table: "hr_business_trip_approvers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_hr_business_trip_travelers_DocumentId",
                table: "hr_business_trip_travelers",
                column: "DocumentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "hr_business_trip_approvers");

            migrationBuilder.DropTable(
                name: "hr_business_trip_travelers");

            migrationBuilder.DropTable(
                name: "hr_business_trip_request_details");
        }
    }
}
