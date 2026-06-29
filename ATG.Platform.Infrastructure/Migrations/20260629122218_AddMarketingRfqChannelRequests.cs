using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATG.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketingRfqChannelRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RfqDocumentFileName",
                table: "marketing_records",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RfqDocumentStorageKey",
                table: "marketing_records",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "marketing_rfq_channel_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MarketingRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    HelpDeskTicketId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkTaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExternalNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_marketing_rfq_channel_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_marketing_rfq_channel_requests_marketing_records_MarketingR~",
                        column: x => x.MarketingRecordId,
                        principalTable: "marketing_records",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_marketing_rfq_channel_requests_users_AssignedUserId",
                        column: x => x.AssignedUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_marketing_rfq_channel_requests_AssignedUserId",
                table: "marketing_rfq_channel_requests",
                column: "AssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_marketing_rfq_channel_requests_DocumentId",
                table: "marketing_rfq_channel_requests",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_marketing_rfq_channel_requests_HelpDeskTicketId",
                table: "marketing_rfq_channel_requests",
                column: "HelpDeskTicketId");

            migrationBuilder.CreateIndex(
                name: "IX_marketing_rfq_channel_requests_MarketingRecordId",
                table: "marketing_rfq_channel_requests",
                column: "MarketingRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_marketing_rfq_channel_requests_WorkTaskId",
                table: "marketing_rfq_channel_requests",
                column: "WorkTaskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "marketing_rfq_channel_requests");

            migrationBuilder.DropColumn(
                name: "RfqDocumentFileName",
                table: "marketing_records");

            migrationBuilder.DropColumn(
                name: "RfqDocumentStorageKey",
                table: "marketing_records");
        }
    }
}
