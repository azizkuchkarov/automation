using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATG.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IncomingLetterHelpDeskTranslation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LinkedDocumentId",
                table: "tickets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceLanguage",
                table: "tickets",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TranslatingLanguage",
                table: "tickets",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HelpDeskTicketId",
                table: "incoming_letter_details",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceLanguage",
                table: "incoming_letter_details",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TranslatingLanguage",
                table: "incoming_letter_details",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LinkedDocumentId",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "SourceLanguage",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "TranslatingLanguage",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "HelpDeskTicketId",
                table: "incoming_letter_details");

            migrationBuilder.DropColumn(
                name: "SourceLanguage",
                table: "incoming_letter_details");

            migrationBuilder.DropColumn(
                name: "TranslatingLanguage",
                table: "incoming_letter_details");
        }
    }
}
