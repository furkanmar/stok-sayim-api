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
            migrationBuilder.AddColumn<int>(
                name: "PricesAdded",
                table: "SecMarketSyncLogs",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PricesAdded",
                table: "SecMarketSyncLogs");
        }
    }
}
