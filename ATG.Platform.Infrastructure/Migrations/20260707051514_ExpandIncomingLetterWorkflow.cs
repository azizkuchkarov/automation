using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATG.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExpandIncomingLetterWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ForInformation",
                table: "incoming_letter_recipients",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "incoming_letter_details",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignmentTask",
                table: "incoming_letter_details",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "incoming_letter_details",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExecutorAcceptedAt",
                table: "incoming_letter_details",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReportedAt",
                table: "incoming_letter_details",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresResponse",
                table: "incoming_letter_details",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresTranslation",
                table: "incoming_letter_details",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ResolutionManagerId",
                table: "incoming_letter_details",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "incoming_letter_details",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SentForResolutionAt",
                table: "incoming_letter_details",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SentToTranslationAt",
                table: "incoming_letter_details",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TranslationReturnedAt",
                table: "incoming_letter_details",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_incoming_letter_details_ResolutionManagerId",
                table: "incoming_letter_details",
                column: "ResolutionManagerId");

            migrationBuilder.AddForeignKey(
                name: "FK_incoming_letter_details_users_ResolutionManagerId",
                table: "incoming_letter_details",
                column: "ResolutionManagerId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.Sql("""
                UPDATE incoming_letter_details SET "Phase" = 'AwaitingResolution' WHERE "Phase" = 'Informed';
                UPDATE incoming_letter_recipients SET "ForInformation" = TRUE;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_incoming_letter_details_users_ResolutionManagerId",
                table: "incoming_letter_details");

            migrationBuilder.DropIndex(
                name: "IX_incoming_letter_details_ResolutionManagerId",
                table: "incoming_letter_details");

            migrationBuilder.DropColumn(
                name: "ForInformation",
                table: "incoming_letter_recipients");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "incoming_letter_details");

            migrationBuilder.DropColumn(
                name: "AssignmentTask",
                table: "incoming_letter_details");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "incoming_letter_details");

            migrationBuilder.DropColumn(
                name: "ExecutorAcceptedAt",
                table: "incoming_letter_details");

            migrationBuilder.DropColumn(
                name: "ReportedAt",
                table: "incoming_letter_details");

            migrationBuilder.DropColumn(
                name: "RequiresResponse",
                table: "incoming_letter_details");

            migrationBuilder.DropColumn(
                name: "RequiresTranslation",
                table: "incoming_letter_details");

            migrationBuilder.DropColumn(
                name: "ResolutionManagerId",
                table: "incoming_letter_details");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "incoming_letter_details");

            migrationBuilder.DropColumn(
                name: "SentForResolutionAt",
                table: "incoming_letter_details");

            migrationBuilder.DropColumn(
                name: "SentToTranslationAt",
                table: "incoming_letter_details");

            migrationBuilder.DropColumn(
                name: "TranslationReturnedAt",
                table: "incoming_letter_details");
        }
    }
}
