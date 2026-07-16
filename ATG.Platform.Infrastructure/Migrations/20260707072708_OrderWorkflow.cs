using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATG.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OrderWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "order_details",
                columns: table => new
                {
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Phase = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DeptHeadId = table.Column<Guid>(type: "uuid", nullable: true),
                    LegalHeadId = table.Column<Guid>(type: "uuid", nullable: true),
                    SupervisingDeputyId = table.Column<Guid>(type: "uuid", nullable: true),
                    FirstDeputyId = table.Column<Guid>(type: "uuid", nullable: true),
                    GeneralDirectorId = table.Column<Guid>(type: "uuid", nullable: true),
                    RevisionNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ScanAttachmentFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ScanAttachmentStorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeptHeadApprovedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LegalApprovedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SupervisingDeputyApprovedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    FirstDeputyApprovedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GeneralDirectorApprovedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    EdsFinalizedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SentToRegistrarAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    PaperSignedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ScanUploadedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DistributedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ArchivedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CoordinationCompletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_details", x => x.DocumentId);
                    table.ForeignKey(
                        name: "FK_order_details_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_order_details_users_DeptHeadId",
                        column: x => x.DeptHeadId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_order_details_users_FirstDeputyId",
                        column: x => x.FirstDeputyId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_order_details_users_GeneralDirectorId",
                        column: x => x.GeneralDirectorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_order_details_users_LegalHeadId",
                        column: x => x.LegalHeadId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_order_details_users_SupervisingDeputyId",
                        column: x => x.SupervisingDeputyId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "order_comments",
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
                    table.PrimaryKey("PK_order_comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_order_comments_order_details_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "order_details",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_order_comments_users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_coordinators",
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
                    table.PrimaryKey("PK_order_coordinators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_order_coordinators_order_details_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "order_details",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_order_coordinators_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_recipients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    NotifiedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_recipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_order_recipients_order_details_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "order_details",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_order_recipients_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_order_comments_AuthorId",
                table: "order_comments",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_order_comments_DocumentId",
                table: "order_comments",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_order_coordinators_DocumentId",
                table: "order_coordinators",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_order_coordinators_UserId",
                table: "order_coordinators",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_order_details_DeptHeadId",
                table: "order_details",
                column: "DeptHeadId");

            migrationBuilder.CreateIndex(
                name: "IX_order_details_FirstDeputyId",
                table: "order_details",
                column: "FirstDeputyId");

            migrationBuilder.CreateIndex(
                name: "IX_order_details_GeneralDirectorId",
                table: "order_details",
                column: "GeneralDirectorId");

            migrationBuilder.CreateIndex(
                name: "IX_order_details_LegalHeadId",
                table: "order_details",
                column: "LegalHeadId");

            migrationBuilder.CreateIndex(
                name: "IX_order_details_SupervisingDeputyId",
                table: "order_details",
                column: "SupervisingDeputyId");

            migrationBuilder.CreateIndex(
                name: "IX_order_recipients_DocumentId",
                table: "order_recipients",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_order_recipients_UserId",
                table: "order_recipients",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_comments");

            migrationBuilder.DropTable(
                name: "order_coordinators");

            migrationBuilder.DropTable(
                name: "order_recipients");

            migrationBuilder.DropTable(
                name: "order_details");
        }
    }
}
