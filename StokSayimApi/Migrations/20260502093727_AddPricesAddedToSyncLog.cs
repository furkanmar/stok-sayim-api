using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StokSayimApi.Migrations
{
    /// <inheritdoc />
    public partial class AddPricesAddedToSyncLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sales_Branches_BranchId",
                table: "Sales");

            migrationBuilder.DropForeignKey(
                name: "FK_Sales_Users_UserId",
                table: "Sales");

            migrationBuilder.AddColumn<int>(
                name: "PricesAdded",
                table: "SecMarketSyncLogs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Users_CompanyId",
                table: "Users",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBranches_BranchId",
                table: "UserBranches",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCounts_BranchId",
                table: "StockCounts",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCounts_CreatedByUserId",
                table: "StockCounts",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountItems_ProductId",
                table: "StockCountItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_UserId",
                table: "Sales",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPrices_OlusturanUserId",
                table: "ProductPrices",
                column: "OlusturanUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_CompanyId",
                table: "Categories",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductPrices_Users_OlusturanUserId",
                table: "ProductPrices",
                column: "OlusturanUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_Branches_BranchId",
                table: "Sales",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_Users_UserId",
                table: "Sales",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductPrices_Users_OlusturanUserId",
                table: "ProductPrices");

            migrationBuilder.DropForeignKey(
                name: "FK_Sales_Branches_BranchId",
                table: "Sales");

            migrationBuilder.DropForeignKey(
                name: "FK_Sales_Users_UserId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Users_CompanyId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_UserBranches_BranchId",
                table: "UserBranches");

            migrationBuilder.DropIndex(
                name: "IX_StockCounts_BranchId",
                table: "StockCounts");

            migrationBuilder.DropIndex(
                name: "IX_StockCounts_CreatedByUserId",
                table: "StockCounts");

            migrationBuilder.DropIndex(
                name: "IX_StockCountItems_ProductId",
                table: "StockCountItems");

            migrationBuilder.DropIndex(
                name: "IX_Sales_UserId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_ProductPrices_OlusturanUserId",
                table: "ProductPrices");

            migrationBuilder.DropIndex(
                name: "IX_Categories_CompanyId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "PricesAdded",
                table: "SecMarketSyncLogs");

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_Branches_BranchId",
                table: "Sales",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_Users_UserId",
                table: "Sales",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
