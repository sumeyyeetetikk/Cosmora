using Cosmora.Context;
using Cosmora.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Cosmora.Services;

public class ProductService : IProductService
{
    private readonly CosmoraDbContext _db;
    public ProductService(CosmoraDbContext db) => _db = db;

    // En çok satan N ürün — SQL'de GROUP BY + TOP
    public async Task<List<TopProductRow>> GetTopProductsAsync(int take = 20)
    {
        return await _db.Sales
            .GroupBy(s => new { s.Product.Name, Category = s.Product.Category.Name })
            .Select(g => new TopProductRow
            {
                Product = g.Key.Name,
                Category = g.Key.Category,
                TotalQuantity = g.Sum(x => (long)x.Quantity),
                TotalRevenue = g.Sum(x => x.TotalAmount)
            })
            .OrderByDescending(x => x.TotalQuantity)
            .Take(take)
            .ToListAsync();
    }

    // Filtreli + sayfalı satış listesi
    public async Task<(List<SaleRow> rows, int totalCount)> GetSalesAsync(SalesListViewModel f)
    {
        // Henüz DB'ye gitmiyor — sadece sorgu kuruluyor
        IQueryable<Models.Sale> q = _db.Sales;

        if (f.CategoryId.HasValue) q = q.Where(s => s.Product.CategoryId == f.CategoryId);
        if (f.CityId.HasValue) q = q.Where(s => s.CityId == f.CityId);
        if (f.From.HasValue) q = q.Where(s => s.OrderDate >= f.From);
        if (f.To.HasValue) q = q.Where(s => s.OrderDate <= f.To);
        if (f.IsCampaign.HasValue) q = q.Where(s => s.IsCampaign == f.IsCampaign);

        // COUNT: SQL'de tek sorgu (kayıtları çekmez)
        int total = await q.CountAsync();

        // Sadece istenen sayfayı çek — OFFSET/FETCH SQL'de
        var rows = await q
            .OrderByDescending(s => s.OrderDate)
            .Skip((f.Page - 1) * f.PageSize)
            .Take(f.PageSize)
            .Select(s => new SaleRow
            {
                Id = s.Id,
                OrderDate = s.OrderDate,
                Product = s.Product.Name,
                Category = s.Product.Category.Name,
                City = s.City.Name,
                Country = s.City.Country,
                Quantity = s.Quantity,
                UnitPrice = s.UnitPrice,
                TotalAmount = s.TotalAmount,
                PaymentMethod = s.PaymentMethod,
                IsCampaign = s.IsCampaign
            })
            .ToListAsync();

        return (rows, total);
    }

    public async Task<List<FilterOption>> GetCategoriesAsync() =>
        await _db.Categories.OrderBy(c => c.Name)
            .Select(c => new FilterOption { Id = c.Id, Name = c.Name }).ToListAsync();

    public async Task<List<FilterOption>> GetCitiesAsync() =>
        await _db.Cities.OrderBy(c => c.Name)
            .Select(c => new FilterOption { Id = c.Id, Name = $"{c.Name} ({c.Country})" }).ToListAsync();
}