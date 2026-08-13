using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LivestockManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseAcquisitionCosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AdditionalAcquisitionCost",
                table: "Purchases",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CostAllocationMethod",
                table: "Purchases",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OtherCostDescription",
                table: "Purchases",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAcquisitionCost",
                table: "Purchases",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCommission",
                table: "Purchases",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalLivestockPurchaseCost",
                table: "Purchases",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalOtherCost",
                table: "Purchases",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalTax",
                table: "Purchases",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalTransportation",
                table: "Purchases",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AdditionalAcquisitionCost",
                table: "PurchaseItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CommissionAmount",
                table: "PurchaseItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CostAllocationMethod",
                table: "PurchaseItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "LivestockPurchaseCost",
                table: "PurchaseItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OtherCostAmount",
                table: "PurchaseItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "OtherCostDescription",
                table: "PurchaseItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                table: "PurchaseItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAcquisitionCost",
                table: "PurchaseItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TransportationAmount",
                table: "PurchaseItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AllocatedCommission",
                table: "Livestock",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AllocatedOtherCost",
                table: "Livestock",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AllocatedTax",
                table: "Livestock",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AllocatedTransportation",
                table: "Livestock",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "OtherCostDescription",
                table: "Livestock",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAcquisitionCost",
                table: "Livestock",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(@"
EXEC(N'ALTER TABLE [Purchases] ADD CONSTRAINT [CK_Purchases_NonNegCosts] CHECK (
    [TotalLivestockPurchaseCost] >= 0 AND
    [TotalCommission] >= 0 AND
    [TotalTax] >= 0 AND
    [TotalTransportation] >= 0 AND
    [TotalOtherCost] >= 0 AND
    [AdditionalAcquisitionCost] >= 0 AND
    [TotalAcquisitionCost] >= 0
)');

EXEC(N'ALTER TABLE [PurchaseItems] ADD CONSTRAINT [CK_PurchaseItems_NonNegCosts] CHECK (
    [LivestockPurchaseCost] >= 0 AND
    [CommissionAmount] >= 0 AND
    [TaxAmount] >= 0 AND
    [TransportationAmount] >= 0 AND
    [OtherCostAmount] >= 0 AND
    [AdditionalAcquisitionCost] >= 0 AND
    [TotalAcquisitionCost] >= 0
)');

EXEC(N'ALTER TABLE [Livestock] ADD CONSTRAINT [CK_Livestock_NonNegAcqCosts] CHECK (
    [AllocatedCommission] >= 0 AND
    [AllocatedTax] >= 0 AND
    [AllocatedTransportation] >= 0 AND
    [AllocatedOtherCost] >= 0 AND
    [TotalAcquisitionCost] >= 0
)');
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
EXEC(N'ALTER TABLE [Purchases] DROP CONSTRAINT IF EXISTS [CK_Purchases_NonNegCosts]');
EXEC(N'ALTER TABLE [PurchaseItems] DROP CONSTRAINT IF EXISTS [CK_PurchaseItems_NonNegCosts]');
EXEC(N'ALTER TABLE [Livestock] DROP CONSTRAINT IF EXISTS [CK_Livestock_NonNegAcqCosts]');
");

            migrationBuilder.DropColumn(
                name: "AdditionalAcquisitionCost",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "CostAllocationMethod",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "OtherCostDescription",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "TotalAcquisitionCost",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "TotalCommission",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "TotalLivestockPurchaseCost",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "TotalOtherCost",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "TotalTax",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "TotalTransportation",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "AdditionalAcquisitionCost",
                table: "PurchaseItems");

            migrationBuilder.DropColumn(
                name: "CommissionAmount",
                table: "PurchaseItems");

            migrationBuilder.DropColumn(
                name: "CostAllocationMethod",
                table: "PurchaseItems");

            migrationBuilder.DropColumn(
                name: "LivestockPurchaseCost",
                table: "PurchaseItems");

            migrationBuilder.DropColumn(
                name: "OtherCostAmount",
                table: "PurchaseItems");

            migrationBuilder.DropColumn(
                name: "OtherCostDescription",
                table: "PurchaseItems");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                table: "PurchaseItems");

            migrationBuilder.DropColumn(
                name: "TotalAcquisitionCost",
                table: "PurchaseItems");

            migrationBuilder.DropColumn(
                name: "TransportationAmount",
                table: "PurchaseItems");

            migrationBuilder.DropColumn(
                name: "AllocatedCommission",
                table: "Livestock");

            migrationBuilder.DropColumn(
                name: "AllocatedOtherCost",
                table: "Livestock");

            migrationBuilder.DropColumn(
                name: "AllocatedTax",
                table: "Livestock");

            migrationBuilder.DropColumn(
                name: "AllocatedTransportation",
                table: "Livestock");

            migrationBuilder.DropColumn(
                name: "OtherCostDescription",
                table: "Livestock");

            migrationBuilder.DropColumn(
                name: "TotalAcquisitionCost",
                table: "Livestock");
        }
    }
}
