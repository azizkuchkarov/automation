using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATG.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OutgoingLetterWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "outgoing_letter_details",
                columns: table => new
                {
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Phase = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RequiresTranslation = table.Column<bool>(type: "boolean", nullable: false),
                    HelpDeskTicketId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceLanguage = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    TranslatingLanguage = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TranslatedAttachmentFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    TranslatedAttachmentStorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DeptHeadId = table.Column<Guid>(type: "uuid", nullable: true),
                    SupervisingDeputyId = table.Column<Guid>(type: "uuid", nullable: true),
                    FirstDeputyId = table.Column<Guid>(type: "uuid", nullable: true),
                    GeneralDirectorId = table.Column<Guid>(type: "uuid", nullable: true),
                    RevisionNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SentToTranslationAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TranslationReturnedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SubmittedToEdsAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeptHeadApprovedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CoordinationCompletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SupervisingDeputyApprovedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    FirstDeputyApprovedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GeneralDirectorApprovedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    EdsFinalizedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SentToRegistrarAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    PaperSignedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DispatchedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ArchivedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outgoing_letter_details", x => x.DocumentId);
                    table.ForeignKey(
                        name: "FK_outgoing_letter_details_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_outgoing_letter_details_users_DeptHeadId",
                        column: x => x.DeptHeadId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_outgoing_letter_details_users_FirstDeputyId",
                        column: x => x.FirstDeputyId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_outgoing_letter_details_users_GeneralDirectorId",
                        column: x => x.GeneralDirectorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_outgoing_letter_details_users_SupervisingDeputyId",
                        column: x => x.SupervisingDeputyId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "outgoing_letter_comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outgoing_letter_comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_outgoing_letter_comments_outgoing_letter_details_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "outgoing_letter_details",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_outgoing_letter_comments_users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "outgoing_letter_coordinators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ForDepartment = table.Column<bool>(type: "boolean", nullable: false),
                    CoordinatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outgoing_letter_coordinators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_outgoing_letter_coordinators_outgoing_letter_details_Docume~",
                        column: x => x.DocumentId,
                        principalTable: "outgoing_letter_details",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_outgoing_letter_coordinators_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_outgoing_letter_comments_AuthorId",
                table: "outgoing_letter_comments",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_outgoing_letter_comments_DocumentId",
                table: "outgoing_letter_comments",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_outgoing_letter_coordinators_DocumentId",
                table: "outgoing_letter_coordinators",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_outgoing_letter_coordinators_UserId",
                table: "outgoing_letter_coordinators",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_outgoing_letter_details_DeptHeadId",
                table: "outgoing_letter_details",
                column: "DeptHeadId");

            migrationBuilder.CreateIndex(
                name: "IX_outgoing_letter_details_FirstDeputyId",
                table: "outgoing_letter_details",
                column: "FirstDeputyId");

            migrationBuilder.CreateIndex(
                name: "IX_outgoing_letter_details_GeneralDirectorId",
                table: "outgoing_letter_details",
                column: "GeneralDirectorId");

            migrationBuilder.CreateIndex(
                name: "IX_outgoing_letter_details_SupervisingDeputyId",
                table: "outgoing_letter_details",
                column: "SupervisingDeputyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outgoing_letter_comments");

            migrationBuilder.DropTable(
                name: "outgoing_letter_coordinators");

            migrationBuilder.DropTable(
                name: "outgoing_letter_details");
        }
    }
}
