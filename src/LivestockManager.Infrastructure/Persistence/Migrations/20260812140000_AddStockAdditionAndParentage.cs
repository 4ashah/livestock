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
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Suppliers",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

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

            migrationBuilder.CreateTable(
                name: "LivestockLosses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FarmId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LivestockId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LossNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    LossType = table.Column<int>(type: "int", nullable: false),
                    LossDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Currency = table.Column<int>(type: "int", nullable: false),
                    BookValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SalvageValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    LossAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsReversed = table.Column<bool>(type: "bit", nullable: false),
                    ReversalReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ReversedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LivestockLosses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LivestockLosses_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LivestockLosses_Farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "Farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LivestockLosses_Livestock_LivestockId",
                        column: x => x.LivestockId,
                        principalTable: "Livestock",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_LivestockLosses_CompanyId_LossDate",
                table: "LivestockLosses",
                columns: new[] { "CompanyId", "LossDate" });

            migrationBuilder.CreateIndex(
                name: "IX_LivestockLosses_CompanyId_LossNumber",
                table: "LivestockLosses",
                columns: new[] { "CompanyId", "LossNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LivestockLosses_FarmId",
                table: "LivestockLosses",
                column: "FarmId");

            migrationBuilder.CreateIndex(
                name: "IX_LivestockLosses_LivestockId_IsReversed",
                table: "LivestockLosses",
                columns: new[] { "LivestockId", "IsReversed" });

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

            migrationBuilder.DropTable(
                name: "LivestockLosses");

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
                name: "Notes",
                table: "Suppliers");

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
