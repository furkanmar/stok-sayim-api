namespace StokSayimApi.Models;

public class ProductPrice
{
    public int Id { get; set; }
    public int ProductId { get; set; }

    /// <summary>Fiyatın hangi birim tipine ait olduğu (ADT, KL, PAK vb.)</summary>
    public string UnitType { get; set; } = "ADT";

    public double AlisFiyati { get; set; }
    public double SatisFiyati { get; set; }
    public DateTime GecerlilikTarihi { get; set; } = DateTime.UtcNow;

    /// <summary>Fiyatı giren kullanıcı. Senkronizasyonla eklenenler için null olabilir.</summary>
    public int? OlusturanUserId { get; set; }

    public Product Product { get; set; } = null!;
    public User? OlusturanUser { get; set; }
}
