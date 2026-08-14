using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LivestockManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleCostsReversalPriceSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CommissionAmount",
                table: "Sales",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CostAllocationMethod",
                table: "Sales",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "NetSaleProceeds",
                table: "Sales",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OtherCostAmount",
                table: "Sales",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "OtherCostDescription",
                table: "Sales",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReversalNotes",
                table: "Sales",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReversalReason",
                table: "Sales",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReversedAt",
                table: "Sales",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReversedByUserId",
                table: "Sales",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SellerTaxAmount",
                table: "Sales",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAdditionalSaleCosts",
                table: "Sales",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TransportationAmount",
                table: "Sales",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AllocatedCommission",
                table: "SaleItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AllocatedOtherCost",
                table: "SaleItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AllocatedSellerTax",
                table: "SaleItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AllocatedTransportation",
                table: "SaleItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalSalePrice",
                table: "SaleItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NetSaleProceeds",
                table: "SaleItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PriceSource",
                table: "SaleItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "SuggestedPrice",
                table: "SaleItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SuggestedPriceMethod",
                table: "SaleItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SuggestedRate",
                table: "SaleItems",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SuggestedWeight",
                table: "SaleItems",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SuggestedWeightDate",
                table: "SaleItems",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.Sql(@"
ALTER TABLE [Sales] ADD CONSTRAINT [CK_Sales_CommissionAmount_NonNegative] CHECK ([CommissionAmount] >= 0);
ALTER TABLE [Sales] ADD CONSTRAINT [CK_Sales_SellerTaxAmount_NonNegative] CHECK ([SellerTaxAmount] >= 0);
ALTER TABLE [Sales] ADD CONSTRAINT [CK_Sales_TransportationAmount_NonNegative] CHECK ([TransportationAmount] >= 0);
ALTER TABLE [Sales] ADD CONSTRAINT [CK_Sales_OtherCostAmount_NonNegative] CHECK ([OtherCostAmount] >= 0);
ALTER TABLE [SaleItems] ADD CONSTRAINT [CK_SaleItems_AllocatedCommission_NonNegative] CHECK ([AllocatedCommission] >= 0);
ALTER TABLE [SaleItems] ADD CONSTRAINT [CK_SaleItems_AllocatedSellerTax_NonNegative] CHECK ([AllocatedSellerTax] >= 0);
ALTER TABLE [SaleItems] ADD CONSTRAINT [CK_SaleItems_AllocatedTransportation_NonNegative] CHECK ([AllocatedTransportation] >= 0);
ALTER TABLE [SaleItems] ADD CONSTRAINT [CK_SaleItems_AllocatedOtherCost_NonNegative] CHECK ([AllocatedOtherCost] >= 0);
");
            migrationBuilder.CreateIndex(
                name: "IX_Sales_Status_ReversedAt",
                table: "Sales",
                columns: new[] { "Status", "ReversedAt" });
            migrationBuilder.CreateIndex(
                name: "IX_Sales_CompanyId_Date",
                table: "Sales",
                columns: new[] { "CompanyId", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sales_CompanyId_Date",
                table: "Sales");
            migrationBuilder.DropIndex(
                name: "IX_Sales_Status_ReversedAt",
                table: "Sales");
            migrationBuilder.Sql(@"
ALTER TABLE [SaleItems] DROP CONSTRAINT IF EXISTS [CK_SaleItems_AllocatedOtherCost_NonNegative];
ALTER TABLE [SaleItems] DROP CONSTRAINT IF EXISTS [CK_SaleItems_AllocatedTransportation_NonNegative];
ALTER TABLE [SaleItems] DROP CONSTRAINT IF EXISTS [CK_SaleItems_AllocatedSellerTax_NonNegative];
ALTER TABLE [SaleItems] DROP CONSTRAINT IF EXISTS [CK_SaleItems_AllocatedCommission_NonNegative];
ALTER TABLE [Sales] DROP CONSTRAINT IF EXISTS [CK_Sales_OtherCostAmount_NonNegative];
ALTER TABLE [Sales] DROP CONSTRAINT IF EXISTS [CK_Sales_TransportationAmount_NonNegative];
ALTER TABLE [Sales] DROP CONSTRAINT IF EXISTS [CK_Sales_SellerTaxAmount_NonNegative];
ALTER TABLE [Sales] DROP CONSTRAINT IF EXISTS [CK_Sales_CommissionAmount_NonNegative];
");
            migrationBuilder.DropColumn(
                name: "CommissionAmount",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "CostAllocationMethod",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "NetSaleProceeds",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "OtherCostAmount",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "OtherCostDescription",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "ReversalNotes",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "ReversalReason",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "ReversedAt",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "ReversedByUserId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "SellerTaxAmount",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "TotalAdditionalSaleCosts",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "TransportationAmount",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "AllocatedCommission",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "AllocatedOtherCost",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "AllocatedSellerTax",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "AllocatedTransportation",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "FinalSalePrice",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "NetSaleProceeds",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "PriceSource",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "SuggestedPrice",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "SuggestedPriceMethod",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "SuggestedRate",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "SuggestedWeight",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "SuggestedWeightDate",
                table: "SaleItems");
        }
    }
}
