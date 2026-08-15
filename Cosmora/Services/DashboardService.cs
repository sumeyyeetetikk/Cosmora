using Cosmora.Context;
using Cosmora.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Cosmora.Services;

public class DashboardService : IDashboardService
{
    private readonly CosmoraDbContext _db;
    public DashboardService(CosmoraDbContext db) => _db = db;

    public async Task<DashboardViewModel> GetDashboardAsync()
    {
        var vm = new DashboardViewModel();

        // --- KPI'lar: her biri tek bir SQL aggregate ---
        vm.TotalRevenue = await _db.Sales.SumAsync(s => s.TotalAmount);
        vm.TotalQuantity = await _db.Sales.SumAsync(s => (long)s.Quantity);
        vm.TotalOrders = await _db.Sales.LongCountAsync();
        vm.AvgBasket = await _db.Sales.AverageAsync(s => s.TotalAmount);

        // --- Aylık trend (mevsimsellik burada görünür) ---
        var monthly = await _db.Sales
            .GroupBy(s => new { s.OrderDate.Year, s.OrderDate.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Revenue = g.Sum(x => x.TotalAmount),
                Quantity = g.Sum(x => (long)x.Quantity)
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync();

        vm.MonthlyTrend = monthly.Select(m => new MonthlyPoint
        {
            Label = $"{m.Year}-{m.Month:D2}",
            Revenue = m.Revenue,
            Quantity = m.Quantity
        }).ToList();

        // --- Kategori dağılımı (ciroya göre) ---
        vm.ByCategory = await _db.Sales
            .GroupBy(s => s.Product.Category.Name)
            .Select(g => new LabelValue { Label = g.Key, Value = g.Sum(x => x.TotalAmount) })
            .OrderByDescending(x => x.Value)
            .ToListAsync();

        // --- En çok satan 10 ürün (adete göre) ---
        vm.TopProducts = await _db.Sales
            .GroupBy(s => s.Product.Name)
            .Select(g => new LabelValue { Label = g.Key, Value = g.Sum(x => (long)x.Quantity) })
            .OrderByDescending(x => x.Value)
            .Take(10)
            .ToListAsync();

        // --- Ülke bazlı ciro ---
        vm.ByCountry = await _db.Sales
            .GroupBy(s => s.City.Country)
            .Select(g => new LabelValue { Label = g.Key, Value = g.Sum(x => x.TotalAmount) })
            .OrderByDescending(x => x.Value)
            .ToListAsync();

        // --- Ödeme yöntemi dağılımı (sipariş sayısı) ---
        vm.ByPayment = await _db.Sales
            .GroupBy(s => s.PaymentMethod)
            .Select(g => new LabelValue { Label = g.Key.ToString(), Value = g.LongCount() })
            .ToListAsync();

        // --- Kampanyalı vs kampanyasız (ciro) ---
        vm.CampaignCompare = await _db.Sales
            .GroupBy(s => s.IsCampaign)
            .Select(g => new LabelValue
            {
                Label = g.Key ? "Kampanyalı" : "Kampanyasız",
                Value = g.Sum(x => x.TotalAmount)
            })
            .ToListAsync();

        return vm;
    }
}