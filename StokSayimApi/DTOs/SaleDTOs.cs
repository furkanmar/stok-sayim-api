namespace StokSayimApi.DTOs;

// ─── Request ─────────────────────────────────────────────────────────────────

public class CreateSaleItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string UnitType { get; set; } = "ADT";
    public decimal Quantity { get; set; }
    public decimal SatisFiyati { get; set; }
    public int KdvOrani { get; set; }
    public decimal LineTotal { get; set; }
}

public class CreateSalePaymentDto
{
    public string Type { get; set; } = "cash";  // "cash" | "card"
    public decimal Amount { get; set; }
}

public class CreateSaleDto
{
    public int BranchId { get; set; }
    public List<CreateSaleItemDto> Items { get; set; } = new();
    public List<CreateSalePaymentDto> Payments { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal GrandTotal { get; set; }
}

public class RefundItemDto
{
    public int SaleItemId { get; set; }
    public decimal Quantity { get; set; }
}

public class RefundSaleDto
{
    public int SaleId { get; set; }
    public List<RefundItemDto> Items { get; set; } = new();
    public string RefundPaymentType { get; set; } = "cash";
}

// ─── Response ────────────────────────────────────────────────────────────────

public class SaleItemResponseDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string UnitType { get; set; } = "ADT";
    public decimal Quantity { get; set; }
    public decimal SatisFiyati { get; set; }
    public int KdvOrani { get; set; }
    public decimal LineTotal { get; set; }
}

public class SalePaymentResponseDto
{
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class SaleResponseDto
{
    public int Id { get; set; }
    public string ReceiptNo { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public bool IsRefunded { get; set; }
    public List<SaleItemResponseDto> Items { get; set; } = new();
    public List<SalePaymentResponseDto> Payments { get; set; } = new();
}

// ─── Z Raporu ────────────────────────────────────────────────────────────────

public class KdvLineDto
{
    public int KdvOrani { get; set; }
    public decimal NetTotal { get; set; }
    public decimal KdvTotal { get; set; }
    public decimal GrossTotal { get; set; }
}

public class ZReportDto
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public int SaleCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal CashTotal { get; set; }
    public decimal CardTotal { get; set; }
    public decimal RefundTotal { get; set; }
    public decimal NetRevenue { get; set; }
    public List<KdvLineDto> KdvLines { get; set; } = new();
}
