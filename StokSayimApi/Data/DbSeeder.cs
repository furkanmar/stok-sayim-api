using Microsoft.EntityFrameworkCore;
using StokSayimApi.Models;
using StokSayimApi.Services;

namespace StokSayimApi.Data;

public static class DbSeeder
{
    private const string SecMarketDbPath = "/app/data/secmarket.db";

    public static async Task SeedAsync(AppDbContext db, SyncService syncService, ILogger logger)
    {
        if (await db.Companies.AnyAsync())
        {
            logger.LogInformation("Database already seeded, skipping basic seed");
            await TrySyncSecMarketAsync(db, syncService, logger);
            return;
        }

        logger.LogInformation("Seeding database...");

        var company = new Company { Name = "Varsayılan Şirket" };
        db.Companies.Add(company);
        await db.SaveChangesAsync();

        var branch = new Branch { Name = "Merkez Şube", CompanyId = company.Id };
        db.Branches.Add(branch);
        await db.SaveChangesAsync();

        var superAdmin = new User
        {
            Username = "superadmin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CHANGE_ME_ADMIN_PASSWORD"),
            Role = "superadmin",
            CompanyId = company.Id,
            UserBranches = new List<UserBranch>
            {
                new UserBranch { BranchId = branch.Id }
            }
        };
        db.Users.Add(superAdmin);
        await db.SaveChangesAsync();

        // Kategoriler
        var categories = new List<(string Name, List<string> Subs)>
        {
            ("İçecek", new List<string>
            {
                "Su", "Maden Suyu & Soda", "Meyve Suyu", "Gazlı İçecek", "Enerji İçeceği",
                "Süt", "Bitkisel Süt", "Çay", "Kahve", "Şalgam & Ayran", "Hoşaf & Komposto"
            }),
            ("Atıştırmalık & Şekerleme", new List<string>
            {
                "Çikolata", "Gofret", "Cips & Kraker", "Kuruyemiş", "Sakız", "Şeker & Lolipop",
                "Bisküvi", "Kek & Pasta", "Lokum & Helva", "Meyve Kurusu"
            }),
            ("Süt & Süt Ürünleri", new List<string>
            {
                "Süt", "Yoğurt", "Peynir", "Beyaz Peynir", "Kaşar & Dil Peyniri",
                "Tereyağı", "Margarin", "Kaymak & Krema", "Ayran", "Kefir"
            }),
            ("Et & Et Ürünleri", new List<string>
            {
                "Kırmızı Et", "Tavuk & Kanatlı", "Balık & Deniz Ürünleri",
                "Salam & Sosis", "Sucuk & Pastırma", "Hindi Ürünleri", "Hazır Köfte"
            }),
            ("Ekmek & Unlu Mamuller", new List<string>
            {
                "Ekmek", "Simit & Açma", "Poğaça & Börek", "Sandviç Ekmeği",
                "Tost Ekmeği", "Pasta & Kek", "Kruvasan", "Pide & Lavaş"
            }),
            ("Meyve & Sebze", new List<string>
            {
                "Taze Meyve", "Taze Sebze", "Yeşillik & Salata", "Patates & Soğan & Sarımsak",
                "Mantar", "Meyve Kurusu", "Zeytin"
            }),
            ("Bakliyat & Tahıl & Kuru Gıda", new List<string>
            {
                "Pirinç", "Bulgur & Kuskus", "Mercimek", "Nohut", "Kuru Fasulye",
                "Makarna", "Erişte & Şehriye", "Un & İrmik", "Mısır Unu & Nişasta"
            }),
            ("Yağ & Sos & Baharat", new List<string>
            {
                "Zeytinyağı", "Ayçiçek Yağı", "Mısırözü Yağı", "Salça", "Ketçap & Mayonez",
                "Hardal & Sos", "Baharat", "Tuz & Karabiber", "Sirke", "Nar Ekşisi"
            }),
            ("Kahvaltılık & Reçel", new List<string>
            {
                "Reçel & Marmelat", "Bal & Pekmez", "Fıstık Ezmesi & Tahin",
                "Çikolatalı Krema", "Zeytin", "Turşu", "Konserve Ürünler"
            }),
            ("Dondurulmuş Ürünler", new List<string>
            {
                "Dondurulmuş Sebze", "Dondurulmuş Et & Balık",
                "Hazır Yemek", "Dondurma", "Pizza & Börek (Dondurulmuş)"
            }),
            ("Temizlik Ürünleri", new List<string>
            {
                "Çamaşır Deterjanı", "Çamaşır Suyu & Yumuşatıcı", "Bulaşık Deterjanı",
                "Yer Temizleyici", "Tuvalet Temizleyici", "Cam & Yüzey Temizleyici",
                "Dezenfektan", "Çöp Poşeti", "Kağıt Havlu & Peçete"
            }),
            ("Kişisel Bakım", new List<string>
            {
                "Şampuan & Saç Bakım", "Sabun & Duş Jeli", "Diş Macunu & Fırçası",
                "Deodorant", "Cilt Bakım & Krem", "Tıraş Ürünleri",
                "Islak Mendil", "Makyaj & Kozmetik"
            }),
            ("Bebek & Çocuk", new List<string>
            {
                "Bebek Bezi", "Islak Mendil (Bebek)", "Bebek Maması", "Bebek Şampuanı & Losyonu",
                "Çocuk Atıştırmalığı", "Biberon & Aksesuar"
            }),
            ("Evcil Hayvan", new List<string>
            {
                "Kedi Maması", "Köpek Maması", "Kuş Maması", "Evcil Hayvan Aksesuarı"
            }),
            ("Kırtasiye & Ev Gereçleri", new List<string>
            {
                "Pil & Şarj", "Ampul & Aydınlatma", "Naylon & Poşet", "Folyo & Streç Film",
                "Kağıt Ürünleri", "Kalem & Defter", "Yapıştırıcı & Bant"
            }),
        };

        foreach (var (name, subs) in categories)
        {
            var category = new Category
            {
                Name = name,
                CompanyId = company.Id,
                SubCategories = subs.Select(s => new SubCategory { Name = s }).ToList()
            };
            db.Categories.Add(category);
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Database seeded — company: {Company}, categories: {Count}", company.Name, categories.Count);

        await TrySyncSecMarketAsync(db, syncService, logger);
    }

    private static async Task TrySyncSecMarketAsync(AppDbContext db, SyncService syncService, ILogger logger)
    {
        if (!File.Exists(SecMarketDbPath))
        {
            logger.LogInformation("secmarket.db not found at {Path}, skipping initial sync", SecMarketDbPath);
            return;
        }

        if (await db.SecMarketSyncLogs.AnyAsync())
        {
            logger.LogInformation("SecMarket sync already done, skipping");
            return;
        }

        try
        {
            logger.LogInformation("secmarket.db found, starting initial sync...");
            var company = await db.Companies.FirstAsync();
            var result = await syncService.SyncFromSqliteAsync(SecMarketDbPath, company.Id);
            logger.LogInformation(
                "Initial sync complete — products: {P}, barcodes: {B}, skipped: {S}",
                result.ProductsAdded, result.BarcodesAdded, result.BarcodesSkipped);
        }
        catch (Exception ex)
        {
            // Sync hatası uygulamanın ayağa kalkmasını engellememeli.
            // Import ekranından veya HTTP endpoint üzerinden tekrar denenebilir.
            logger.LogError(ex, "Initial secmarket.db sync failed — app will start without product data");
        }
    }
}