using Cosmora.Models.Enums;

namespace Cosmora.Models.ViewModels;

public class SalesListViewModel
{
    // Filtre alanları (formdan gelir, geri doldurmak için de kullanılır)
    public int? CategoryId { get; set; }
    public int? CityId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public bool? IsCampaign { get; set; }

    // Sayfalama
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    // Sonuçlar
    public List<SaleRow> Rows { get; set; } = new();
    public List<TopProductRow> TopProducts { get; set; } = new();

    // Dropdown kaynakları
    public List<FilterOption> Categories { get; set; } = new();
    public List<FilterOption> Cities { get; set; } = new();
}

public class SaleRow
{
    public long Id { get; set; }
    public DateTime OrderDate { get; set; }
    public string Product { get; set; } = "";
    public string Category { get; set; } = "";
    public string City { get; set; } = "";
    public string Country { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public bool IsCampaign { get; set; }
}

public class TopProductRow
{
    public string Product { get; set; } = "";
    public string Category { get; set; } = "";
    public long TotalQuantity { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class FilterOption
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}