using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATG.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IncomingLetterTranslatedAttachment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TranslatedAttachmentFileName",
                table: "incoming_letter_details",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TranslatedAttachmentStorageKey",
                table: "incoming_letter_details",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TranslatedAttachmentFileName",
                table: "incoming_letter_details");

            migrationBuilder.DropColumn(
                name: "TranslatedAttachmentStorageKey",
                table: "incoming_letter_details");
        }
    }
}
