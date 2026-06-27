using ATG.Platform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATG.Platform.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260627130000_AddProcurementAttachmentStorageKey")]
public partial class AddProcurementAttachmentStorageKey : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "StorageKey",
            table: "procurement_request_attachments",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "StorageKey",
            table: "procurement_request_attachments");
    }
}
