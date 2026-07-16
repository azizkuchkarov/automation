using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATG.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketingPlanRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RegisteredAt",
                table: "marketing_procurement_plans",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationMethod",
                table: "marketing_procurement_plans",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationNumber",
                table: "marketing_procurement_plans",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TemplateFileName",
                table: "marketing_procurement_plans",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TemplateStorageKey",
                table: "marketing_procurement_plans",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RegisteredAt",
                table: "marketing_procurement_plans");

            migrationBuilder.DropColumn(
                name: "RegistrationMethod",
                table: "marketing_procurement_plans");

            migrationBuilder.DropColumn(
                name: "RegistrationNumber",
                table: "marketing_procurement_plans");

            migrationBuilder.DropColumn(
                name: "TemplateFileName",
                table: "marketing_procurement_plans");

            migrationBuilder.DropColumn(
                name: "TemplateStorageKey",
                table: "marketing_procurement_plans");
        }
    }
}
