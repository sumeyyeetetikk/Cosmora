namespace Cosmora.Models.ViewModels;

public class DashboardViewModel
{
    public decimal TotalRevenue { get; set; }
    public long TotalQuantity { get; set; }
    public long TotalOrders { get; set; }
    public decimal AvgBasket { get; set; }

    public List<MonthlyPoint> MonthlyTrend { get; set; } = new();
    public List<LabelValue> ByCategory { get; set; } = new();
    public List<LabelValue> TopProducts { get; set; } = new();
    public List<LabelValue> ByCountry { get; set; } = new();
    public List<LabelValue> ByPayment { get; set; } = new();
    public List<LabelValue> CampaignCompare { get; set; } = new();
}

public class MonthlyPoint
{
    public string Label { get; set; } = "";   
    public decimal Revenue { get; set; }
    public long Quantity { get; set; }
}

public class LabelValue
{
    public string Label { get; set; } = "";
    public decimal Value { get; set; }
}