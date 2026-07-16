using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATG.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MemoWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "memo_details",
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
                    RequiresTopManagementResolution = table.Column<bool>(type: "boolean", nullable: false),
                    ResolutionManagerId = table.Column<Guid>(type: "uuid", nullable: true),
                    RoutedById = table.Column<Guid>(type: "uuid", nullable: true),
                    RoutedToDepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignmentTask = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DueDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RequiresResponse = table.Column<bool>(type: "boolean", nullable: false),
                    RevisionNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SentToTranslationAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TranslationReturnedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeptHeadApprovedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CoordinationCompletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RoutedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ExecutorAcceptedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ReportedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ArchivedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_memo_details", x => x.DocumentId);
                    table.ForeignKey(
                        name: "FK_memo_details_departments_RoutedToDepartmentId",
                        column: x => x.RoutedToDepartmentId,
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_memo_details_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_memo_details_users_DeptHeadId",
                        column: x => x.DeptHeadId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_memo_details_users_ResolutionManagerId",
                        column: x => x.ResolutionManagerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_memo_details_users_RoutedById",
                        column: x => x.RoutedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "memo_comments",
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
                    table.PrimaryKey("PK_memo_comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_memo_comments_memo_details_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "memo_details",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_memo_comments_users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "memo_coordinators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CoordinatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_memo_coordinators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_memo_coordinators_memo_details_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "memo_details",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_memo_coordinators_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "memo_recipients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ForInformation = table.Column<bool>(type: "boolean", nullable: false),
                    NotifiedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_memo_recipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_memo_recipients_departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_memo_recipients_memo_details_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "memo_details",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_memo_recipients_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_memo_comments_AuthorId",
                table: "memo_comments",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_memo_comments_DocumentId",
                table: "memo_comments",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_memo_coordinators_DocumentId",
                table: "memo_coordinators",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_memo_coordinators_UserId",
                table: "memo_coordinators",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_memo_details_DeptHeadId",
                table: "memo_details",
                column: "DeptHeadId");

            migrationBuilder.CreateIndex(
                name: "IX_memo_details_ResolutionManagerId",
                table: "memo_details",
                column: "ResolutionManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_memo_details_RoutedById",
                table: "memo_details",
                column: "RoutedById");

            migrationBuilder.CreateIndex(
                name: "IX_memo_details_RoutedToDepartmentId",
                table: "memo_details",
                column: "RoutedToDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_memo_recipients_DepartmentId",
                table: "memo_recipients",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_memo_recipients_DocumentId",
                table: "memo_recipients",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_memo_recipients_UserId",
                table: "memo_recipients",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "memo_comments");

            migrationBuilder.DropTable(
                name: "memo_coordinators");

            migrationBuilder.DropTable(
                name: "memo_recipients");

            migrationBuilder.DropTable(
                name: "memo_details");
        }
    }
}
