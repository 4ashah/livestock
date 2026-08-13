using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LivestockManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStockAdditionAndParentage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DateOfBirth",
                table: "Livestock",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FatherLivestockId",
                table: "Livestock",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MotherLivestockId",
                table: "Livestock",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StockSource",
                table: "Livestock",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Livestock_AcquisitionDate",
                table: "Livestock",
                column: "AcquisitionDate");

            migrationBuilder.CreateIndex(
                name: "IX_Livestock_CompanyId_StockSource",
                table: "Livestock",
                columns: new[] { "CompanyId", "StockSource" });

            migrationBuilder.CreateIndex(
                name: "IX_Livestock_DateOfBirth",
                table: "Livestock",
                column: "DateOfBirth");

            migrationBuilder.CreateIndex(
                name: "IX_Livestock_FatherLivestockId",
                table: "Livestock",
                column: "FatherLivestockId");

            migrationBuilder.CreateIndex(
                name: "IX_Livestock_MotherLivestockId",
                table: "Livestock",
                column: "MotherLivestockId");

            migrationBuilder.AddForeignKey(
                name: "FK_Livestock_Livestock_FatherLivestockId",
                table: "Livestock",
                column: "FatherLivestockId",
                principalTable: "Livestock",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Livestock_Livestock_MotherLivestockId",
                table: "Livestock",
                column: "MotherLivestockId",
                principalTable: "Livestock",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Livestock_Livestock_FatherLivestockId",
                table: "Livestock");

            migrationBuilder.DropForeignKey(
                name: "FK_Livestock_Livestock_MotherLivestockId",
                table: "Livestock");

            migrationBuilder.DropIndex(
                name: "IX_Livestock_AcquisitionDate",
                table: "Livestock");

            migrationBuilder.DropIndex(
                name: "IX_Livestock_CompanyId_StockSource",
                table: "Livestock");

            migrationBuilder.DropIndex(
                name: "IX_Livestock_DateOfBirth",
                table: "Livestock");

            migrationBuilder.DropIndex(
                name: "IX_Livestock_FatherLivestockId",
                table: "Livestock");

            migrationBuilder.DropIndex(
                name: "IX_Livestock_MotherLivestockId",
                table: "Livestock");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "Livestock");

            migrationBuilder.DropColumn(
                name: "FatherLivestockId",
                table: "Livestock");

            migrationBuilder.DropColumn(
                name: "MotherLivestockId",
                table: "Livestock");

            migrationBuilder.DropColumn(
                name: "StockSource",
                table: "Livestock");
        }
    }
}
