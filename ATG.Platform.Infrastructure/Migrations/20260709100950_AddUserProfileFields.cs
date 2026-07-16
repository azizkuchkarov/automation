using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATG.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PassportNumber",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PassportSeries",
                table: "users",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProfileCompletedAt",
                table: "users",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "hr_business_trip_travelers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_hr_business_trip_travelers_UserId",
                table: "hr_business_trip_travelers",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_hr_business_trip_travelers_users_UserId",
                table: "hr_business_trip_travelers",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_hr_business_trip_travelers_users_UserId",
                table: "hr_business_trip_travelers");

            migrationBuilder.DropIndex(
                name: "IX_hr_business_trip_travelers_UserId",
                table: "hr_business_trip_travelers");

            migrationBuilder.DropColumn(
                name: "PassportNumber",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PassportSeries",
                table: "users");

            migrationBuilder.DropColumn(
                name: "ProfileCompletedAt",
                table: "users");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "hr_business_trip_travelers");
        }
    }
}
