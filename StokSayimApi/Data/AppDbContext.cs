using Microsoft.EntityFrameworkCore;
using StokSayimApi.Models;

namespace StokSayimApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserBranch> UserBranches => Set<UserBranch>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<SubCategory> SubCategories => Set<SubCategory>();
    public DbSet<StockCount> StockCounts => Set<StockCount>();
    public DbSet<StockCountItem> StockCountItems => Set<StockCountItem>();
    public DbSet<ProductPrice> ProductPrices => Set<ProductPrice>();
    public DbSet<ProductBarcode> ProductBarcodes => Set<ProductBarcode>();
    public DbSet<SecMarketSyncLog> SecMarketSyncLogs => Set<SecMarketSyncLog>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<SalePayment> SalePayments => Set<SalePayment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Product: ProductId is manually assigned (no autoincrement)
        modelBuilder.Entity<Product>()
            .Property(p => p.ProductId)
            .ValueGeneratedNever();

        // UserBranch composite key
        modelBuilder.Entity<UserBranch>()
            .HasKey(ub => new { ub.UserId, ub.BranchId });

        // User: unique username per company
        modelBuilder.Entity<User>()
            .HasIndex(u => new { u.Username, u.CompanyId })
            .IsUnique();

        // StockCountItem: unique product per stock count
        modelBuilder.Entity<StockCountItem>()
            .HasIndex(i => new { i.StockCountId, i.ProductId })
            .IsUnique();

        // ProductBarcode: unique per (company, barcode, unit_type)
        modelBuilder.Entity<ProductBarcode>()
            .HasIndex(pb => new { pb.CompanyId, pb.Barcode, pb.UnitType })
            .IsUnique();

        // ProductBarcode: index for fetching all barcodes of a product
        modelBuilder.Entity<ProductBarcode>()
            .HasIndex(pb => pb.ProductId);

        // ProductPrice: index for fetching prices of a product by unit_type
        modelBuilder.Entity<ProductPrice>()
            .HasIndex(pp => new { pp.ProductId, pp.UnitType });

        // Sale: unique receipt number
        modelBuilder.Entity<Sale>()
            .HasIndex(s => s.ReceiptNo)
            .IsUnique();

        // Sale: index for daily queries (branchId + date)
        modelBuilder.Entity<Sale>()
            .HasIndex(s => new { s.BranchId, s.CreatedAt });

        // Sale: index for company-level queries
        modelBuilder.Entity<Sale>()
            .HasIndex(s => s.CompanyId);

        // SaleItem: decimal precision
        modelBuilder.Entity<SaleItem>()
            .Property(i => i.SatisFiyati)
            .HasPrecision(18, 4);

        modelBuilder.Entity<SaleItem>()
            .Property(i => i.LineTotal)
            .HasPrecision(18, 4);

        modelBuilder.Entity<SaleItem>()
            .Property(i => i.Quantity)
            .HasPrecision(18, 4);

        // Sale: decimal precision
        modelBuilder.Entity<Sale>()
            .Property(s => s.TotalAmount)
            .HasPrecision(18, 4);

        modelBuilder.Entity<Sale>()
            .Property(s => s.DiscountAmount)
            .HasPrecision(18, 4);

        modelBuilder.Entity<Sale>()
            .Property(s => s.GrandTotal)
            .HasPrecision(18, 4);

        // SalePayment: decimal precision
        modelBuilder.Entity<SalePayment>()
            .Property(p => p.Amount)
            .HasPrecision(18, 4);
    }
}
