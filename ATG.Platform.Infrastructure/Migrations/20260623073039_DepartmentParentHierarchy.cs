using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATG.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DepartmentParentHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_departments_OrganizationId",
                table: "departments");

            migrationBuilder.AddColumn<Guid>(
                name: "ParentId",
                table: "departments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_departments_OrganizationId_Code",
                table: "departments",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_departments_ParentId",
                table: "departments",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_departments_departments_ParentId",
                table: "departments",
                column: "ParentId",
                principalTable: "departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_departments_departments_ParentId",
                table: "departments");

            migrationBuilder.DropIndex(
                name: "IX_departments_OrganizationId_Code",
                table: "departments");

            migrationBuilder.DropIndex(
                name: "IX_departments_ParentId",
                table: "departments");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "departments");

            migrationBuilder.CreateIndex(
                name: "IX_departments_OrganizationId",
                table: "departments",
                column: "OrganizationId");
        }
    }
}
