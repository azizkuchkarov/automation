using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATG.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketingOfferEngineerReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EngineerReviewComment",
                table: "marketing_offers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EngineerReviewStatus",
                table: "marketing_offers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "EngineerReviewedAt",
                table: "marketing_offers",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EngineerReviewedById",
                table: "marketing_offers",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EngineerReviewComment",
                table: "marketing_offers");

            migrationBuilder.DropColumn(
                name: "EngineerReviewStatus",
                table: "marketing_offers");

            migrationBuilder.DropColumn(
                name: "EngineerReviewedAt",
                table: "marketing_offers");

            migrationBuilder.DropColumn(
                name: "EngineerReviewedById",
                table: "marketing_offers");
        }
    }
}
