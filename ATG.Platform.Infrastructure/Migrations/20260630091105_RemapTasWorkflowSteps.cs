using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATG.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemapTasWorkflowSteps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Body = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EntityType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActionUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_notifications_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_notifications_UserId_IsRead_CreatedAt",
                table: "user_notifications",
                columns: new[] { "UserId", "IsRead", "CreatedAt" });

            migrationBuilder.Sql("""
                UPDATE procurement_request_details
                SET "CurrentStep" = CASE
                    WHEN "CurrentStep" <= 4 THEN "CurrentStep"
                    WHEN "CurrentStep" IN (5, 6) THEN 5
                    WHEN "CurrentStep" IN (7, 8, 9) THEN 6
                    WHEN "CurrentStep" >= 10 THEN 7
                    ELSE "CurrentStep"
                END
                WHERE "Flow" = 'TechnicalAffairs';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_notifications");
        }
    }
}
