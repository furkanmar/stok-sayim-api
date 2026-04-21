using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StokSayimApi.Migrations
{
    /// <inheritdoc />
    public partial class AddPluToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Plu kolonunu geçici olarak nullable ekle (mevcut satırlar için)
            migrationBuilder.AddColumn<int>(
                name: "Plu",
                table: "Products",
                type: "integer",
                nullable: true);

            // 2. Mevcut ürünlere şirket bazında 1000'den başlayarak ardışık PLU ata
            migrationBuilder.Sql(@"
                WITH numbered AS (
                    SELECT ""Id"",
                           ROW_NUMBER() OVER (
                               PARTITION BY ""CompanyId""
                               ORDER BY ""Id""
                           ) + 999 AS new_plu
                    FROM ""Products""
                )
                UPDATE ""Products"" p
                SET ""Plu"" = n.new_plu
                FROM numbered n
                WHERE p.""Id"" = n.""Id"";
            ");

            // 3. Güvenlik kontrolü: bir şirkette 9999'u aşan olursa migration hata versin
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM ""Products"" WHERE ""Plu"" > 9999) THEN
                        RAISE EXCEPTION 'PLU aralığı (1000-9999) bir şirket için doldu. Alan genişletilmeli.';
                    END IF;
                END $$;
            ");

            // 4. Kolonu NOT NULL yap
            migrationBuilder.AlterColumn<int>(
                name: "Plu",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            // 5. Şirket + PLU üzerinde unique index
            migrationBuilder.CreateIndex(
                name: "IX_Products_CompanyId_Plu",
                table: "Products",
                columns: new[] { "CompanyId", "Plu" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_CompanyId_Plu",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Plu",
                table: "Products");
        }
    }
}
