using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATG.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHrBusinessTripCertificates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CertificateDeliveredAt",
                table: "hr_business_trip_travelers",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CertificateNumber",
                table: "hr_business_trip_travelers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CertificateStorageKey",
                table: "hr_business_trip_travelers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CertificateDeliveredAt",
                table: "hr_business_trip_travelers");

            migrationBuilder.DropColumn(
                name: "CertificateNumber",
                table: "hr_business_trip_travelers");

            migrationBuilder.DropColumn(
                name: "CertificateStorageKey",
                table: "hr_business_trip_travelers");
        }
    }
}
