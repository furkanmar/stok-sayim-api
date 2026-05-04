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
            ("ALKOLLÜ İÇECEKLER", new List<string>
            {
                "ALKOLLÜ İÇKİLER-1",
            }),
            ("ATIŞTIRMALIK ÜRÜNLER", new List<string>
            {
                "ATIŞTIRMALIK ŞEKERLER",
                "BİSKÜVİLER",
                "CİPSLER",
                "EKMEK VE DİĞER UNLU MAMÜLLER",
                "Ekmek ve Unlu Mamüller",
                "KEKLER",
                "KUMANYA KOLISI",
                "KURUYEMİŞ VE KURU MEYVELER",
                "SAKIZLAR",
                "ÇİKOLATA KAPLAMALILAR",
                "ÇİKOLATALAR",
            }),
            ("DONUK ÜRÜNLER", new List<string>
            {
                "DONUK ET",
                "DONUK HİNDİ ETLERİ",
                "DONUK PİLİÇ",
            }),
            ("ET VE ET ÜRÜNLERİ", new List<string>
            {
                "BALIK ve SU ÜRÜNLERİ",
                "KIRMIZI ETLER",
                "KURBANLIKLAR",
                "PİLİÇ ve KANATLILAR",
                "SAKATATLAR",
                "SUCUK,SALAM,SOSİS VE DİĞ.İŞL.Ü",
                "YUMURTALAR",
                "İŞLENMİŞ ET ÜRÜNLERİ",
            }),
            ("GIDA DIŞI ÜRÜNLER", new List<string>
            {
                "ALIŞVERİŞ POŞEDİ",
                "AYDINLATMA ÜRÜNLERİ",
                "BULAŞIK TEMİZLEME GEREÇLERİ",
                "DİĞER EV GEREÇLERİ",
                "DİĞER GIDA DIŞI ÜRÜNLER",
                "EV TEMİZLEME VE BAKIM ÜRÜNLERİ",
                "FOTOKOPİ KAĞITLARI",
                "MUTFAK EŞYA VE GEREÇLERİ",
                "NAYLON-PLASTİK AMBALAJ MALZEME",
                "SPOT ELEKTRONİK",
                "TELEFON KARTLARI",
                "TELEKOMÜNİKASYON",
                "ÇAMAŞIR-GİYSİ BAKIM GEREÇLERİ",
            }),
            ("HEDİYE ÇEKLERİ", new List<string>
            {
                "HEDİYE ÇEKLERİ",
            }),
            ("KAĞIT ÜRÜNLERİ", new List<string>
            {
                "ENDÜSTRİYEL KAĞIT ÜRÜNLERİ",
                "HASTA BEZİ VE DİĞER",
                "HİJYENİK PEDLER",
                "KAĞIT MENDİLLER",
                "KAĞIT PEÇETE ve HAVLULAR",
                "TUVALET KAĞITLARI",
                "ÇOCUK BEZLERİ",
            }),
            ("KRİSTAL ÇUVAL ŞEKER", new List<string>
            {
                "25KG KRİSTAL TOZ ŞEKER",
                "50KG KRİSTAL TOZ ŞEKER",
            }),
            ("KURU GIDA", new List<string>
            {
                "BAKLİYAT",
                "KETÇAP-MAYONEZ-SOSLAR",
                "KONSERVE-TURŞU-HAZIR YEMEKLER",
                "MAKARNA",
                "MAMA VE BESİN ÇEŞİTLERİ",
                "TOZ TATLI-PASTA MALZEMELERİ",
                "UN",
                "ÇORBA-BULYON-TUZ-BAHARAT",
                "ŞEKERLER",
            }),
            ("KİŞİSEL BAKIM ÜRÜNLERİ", new List<string>
            {
                "BEBEK BAKIM ÜRÜNLERİ",
                "CİLT BAKIM ÜRÜNLERİ",
                "DEODORANTLAR- PARFÜMLER VE DİĞ",
                "DİŞ MACUNU ve DİŞ BAKIM ÜRÜNL.",
                "KOLONYALAR",
                "SABUNLAR",
                "SAÇ ŞEKİLLENDİRİCİLER- DİĞER S",
                "TRAŞ KREMLERİ VE TRAŞ ÜRÜNLERİ",
                "ŞAMPUANLAR ve SAÇ KREMLERİ",
            }),
            ("PALETLER", new List<string>
            {
                "PALETLER",
            }),
            ("SEBZE&MEYVE", new List<string>
            {
                "MEYVE",
                "MEYVE SEBZE AMBALAJLARI",
                "SEBZE",
            }),
            ("SICAK İÇECEKLER", new List<string>
            {
                "KAHVE",
                "KAHVE VE KAHVE KREMALARI",
                "SİYAH ÇAYLAR",
                "ÇAY",
                "ÇAYKUR",
            }),
            ("SIVI YAĞ VE MARGARİN", new List<string>
            {
                "AYÇİÇEK YAĞLARI",
                "ENDÜSTRİYEL TİP MARGARİNLER",
                "EV TİPİ MARGARİNLER",
                "KARIŞIM YAĞLAR",
                "MISIR YAĞLARI",
                "ZEYTİN YAĞLARI",
            }),
            ("SOĞUK İÇECEKLER", new List<string>
            {
                "BUZLU ÇAYLAR",
                "ENERJİ İÇECEKLERİ",
                "GAZLI İÇECEKLER",
                "MEYVE SULARI",
                "SODALAR",
                "SULAR",
                "ÖZEL SOĞUK İÇECEKLER",
            }),
            ("SÜT VE SÜT ÜRÜNLERİ", new List<string>
            {
                "DONDURMALAR",
                "DONDURULMUŞ ÜRÜNLER",
                "PEYNİRLER",
                "PUDİNG VE SÜTLÜ TATLILAR",
                "SÜTLER",
                "TEREYAĞ-KREMALAR",
                "YOĞURT-AYRANLAR",
            }),
            ("SİGARALAR VE DİĞER TÜTÜN ÜRÜNLERİ", new List<string>
            {
                "SİGARALAR VE DİĞER TÜTÜN ÜRÜNLERİ",
            }),
            ("TEMİZLİK ÜRÜNLERİ", new List<string>
            {
                "BULAŞIK TEMİZLEME ÜRÜNLERİ",
                "DİĞER TEMİZLEME ÜRÜNLERİ",
                "EV-YÜZEY TEMİZLİME ÜRÜNLERİ",
                "ÇAMAŞIR TEMİZLEME ÜRÜNLERİ",
            }),
            ("ŞARK. VE KAHV. ÜRÜNLERİ", new List<string>
            {
                "FINDIK KREMALARI- FINDIK FISTI",
                "KREM ÇİKOLATA",
                "MEZE ÇEŞİTLERİ",
                "PEKMEZ- TAHİN- HELVA VB.",
                "REÇEL- MARMELAT VE BALLAR",
                "TAHIL GEVREKLERİ",
                "ZEYTİNLER",
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