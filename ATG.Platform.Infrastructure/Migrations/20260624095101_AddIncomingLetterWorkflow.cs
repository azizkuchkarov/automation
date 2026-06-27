using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATG.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIncomingLetterWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "incoming_letter_details",
                columns: table => new
                {
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Phase = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    InformedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RoutedById = table.Column<Guid>(type: "uuid", nullable: true),
                    RoutedToDepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    RoutedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incoming_letter_details", x => x.DocumentId);
                    table.ForeignKey(
                        name: "FK_incoming_letter_details_departments_RoutedToDepartmentId",
                        column: x => x.RoutedToDepartmentId,
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_incoming_letter_details_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_incoming_letter_details_users_RoutedById",
                        column: x => x.RoutedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "incoming_letter_comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incoming_letter_comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_incoming_letter_comments_incoming_letter_details_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "incoming_letter_details",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_incoming_letter_comments_users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "incoming_letter_recipients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Informed = table.Column<bool>(type: "boolean", nullable: false),
                    InformedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incoming_letter_recipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_incoming_letter_recipients_incoming_letter_details_Document~",
                        column: x => x.DocumentId,
                        principalTable: "incoming_letter_details",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_incoming_letter_recipients_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_incoming_letter_comments_AuthorId",
                table: "incoming_letter_comments",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_incoming_letter_comments_DocumentId",
                table: "incoming_letter_comments",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_incoming_letter_details_RoutedById",
                table: "incoming_letter_details",
                column: "RoutedById");

            migrationBuilder.CreateIndex(
                name: "IX_incoming_letter_details_RoutedToDepartmentId",
                table: "incoming_letter_details",
                column: "RoutedToDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_incoming_letter_recipients_DocumentId",
                table: "incoming_letter_recipients",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_incoming_letter_recipients_UserId",
                table: "incoming_letter_recipients",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "incoming_letter_comments");

            migrationBuilder.DropTable(
                name: "incoming_letter_recipients");

            migrationBuilder.DropTable(
                name: "incoming_letter_details");
        }
    }
}
