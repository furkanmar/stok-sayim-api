using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace StokSayimApi.Migrations
{
    /// <inheritdoc />
    public partial class RedesignSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ─── Eski schema tamamen kaldır ──────────────────────────────────────
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"StockCountItems\" CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"StockCounts\" CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"ProductPrices\" CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"ProductBarcodes\" CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"Products\" CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"SubCategories\" CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"Categories\" CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"UserBranches\" CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"Users\" CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"Branches\" CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"Companies\" CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"SecMarketSyncLogs\" CASCADE;");

            // ─── Companies ───────────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Companies", x => x.Id));

            // ─── Branches ────────────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CompanyId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                    table.ForeignKey("FK_Branches_Companies_CompanyId", x => x.CompanyId, "Companies", "Id", onDelete: ReferentialAction.Cascade);
                });

            // ─── Users ───────────────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    CompanyId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey("FK_Users_Companies_CompanyId", x => x.CompanyId, "Companies", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex("IX_Users_Username_CompanyId", "Users", new[] { "Username", "CompanyId" }, unique: true);

            // ─── UserBranches ─────────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "UserBranches",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    BranchId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBranches", x => new { x.UserId, x.BranchId });
                    table.ForeignKey("FK_UserBranches_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_UserBranches_Branches_BranchId", x => x.BranchId, "Branches", "Id", onDelete: ReferentialAction.Cascade);
                });

            // ─── Categories ───────────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CompanyId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey("FK_Categories_Companies_CompanyId", x => x.CompanyId, "Companies", "Id", onDelete: ReferentialAction.Cascade);
                });

            // ─── SubCategories ────────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "SubCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubCategories", x => x.Id);
                    table.ForeignKey("FK_SubCategories_Categories_CategoryId", x => x.CategoryId, "Categories", "Id", onDelete: ReferentialAction.Cascade);
                });

            // ─── Products (ProductId: manuel atanan, autoincrement YOK) ───────────
            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    ProductName = table.Column<string>(type: "text", nullable: false),
                    Unit = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: true),
                    KdvOrani = table.Column<int>(type: "integer", nullable: false, defaultValue: 20),
                    Kategori = table.Column<string>(type: "text", nullable: true),
                    AltKategori = table.Column<string>(type: "text", nullable: true),
                    Marka = table.Column<string>(type: "text", nullable: true),
                    CompanyId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.ProductId);
                    table.ForeignKey("FK_Products_Companies_CompanyId", x => x.CompanyId, "Companies", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex("IX_Products_CompanyId", "Products", "CompanyId");

            // ─── ProductBarcodes ──────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "ProductBarcodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Barcode = table.Column<string>(type: "text", nullable: false),
                    UnitType = table.Column<string>(type: "text", nullable: false, defaultValue: "ADT"),
                    UnitQuantity = table.Column<double>(type: "double precision", nullable: true),
                    CompanyId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductBarcodes", x => x.Id);
                    table.ForeignKey("FK_ProductBarcodes_Products_ProductId", x => x.ProductId, "Products", "ProductId", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex("IX_ProductBarcodes_ProductId", "ProductBarcodes", "ProductId");
            migrationBuilder.CreateIndex("IX_ProductBarcodes_CompanyId_Barcode_UnitType", "ProductBarcodes",
                new[] { "CompanyId", "Barcode", "UnitType" }, unique: true);

            // ─── ProductPrices ────────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "ProductPrices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    UnitType = table.Column<string>(type: "text", nullable: false, defaultValue: "ADT"),
                    AlisFiyati = table.Column<double>(type: "double precision", nullable: false),
                    SatisFiyati = table.Column<double>(type: "double precision", nullable: false),
                    GecerlilikTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OlusturanUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductPrices", x => x.Id);
                    table.ForeignKey("FK_ProductPrices_Products_ProductId", x => x.ProductId, "Products", "ProductId", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_ProductPrices_Users_OlusturanUserId", x => x.OlusturanUserId, "Users", "Id", onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex("IX_ProductPrices_ProductId_UnitType", "ProductPrices",
                new[] { "ProductId", "UnitType" });

            // ─── StockCounts ──────────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "StockCounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BranchId = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false, defaultValue: "active"),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockCounts", x => x.Id);
                    table.ForeignKey("FK_StockCounts_Branches_BranchId", x => x.BranchId, "Branches", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_StockCounts_Users_CreatedByUserId", x => x.CreatedByUserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
                });

            // ─── StockCountItems ──────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "StockCountItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StockCountId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Stock = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockCountItems", x => x.Id);
                    table.ForeignKey("FK_StockCountItems_StockCounts_StockCountId", x => x.StockCountId, "StockCounts", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_StockCountItems_Products_ProductId", x => x.ProductId, "Products", "ProductId", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex("IX_StockCountItems_StockCountId_ProductId", "StockCountItems",
                new[] { "StockCountId", "ProductId" }, unique: true);

            // ─── SecMarketSyncLogs ────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "SecMarketSyncLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SourceFile = table.Column<string>(type: "text", nullable: false),
                    ProductsAdded = table.Column<int>(type: "integer", nullable: false),
                    BarcodesAdded = table.Column<int>(type: "integer", nullable: false),
                    BarcodesSkipped = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false, defaultValue: "success"),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_SecMarketSyncLogs", x => x.Id));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("StockCountItems");
            migrationBuilder.DropTable("StockCounts");
            migrationBuilder.DropTable("ProductPrices");
            migrationBuilder.DropTable("ProductBarcodes");
            migrationBuilder.DropTable("Products");
            migrationBuilder.DropTable("SecMarketSyncLogs");
            migrationBuilder.DropTable("SubCategories");
            migrationBuilder.DropTable("Categories");
            migrationBuilder.DropTable("UserBranches");
            migrationBuilder.DropTable("Users");
            migrationBuilder.DropTable("Branches");
            migrationBuilder.DropTable("Companies");
        }
    }
}
