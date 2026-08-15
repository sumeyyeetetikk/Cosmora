using Cosmora.Models.ViewModels;
using Cosmora.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cosmora.Controllers;

public class ProductsController : Controller
{
    private readonly IProductService _svc;
    public ProductsController(IProductService svc) => _svc = svc;

    public async Task<IActionResult> Index(SalesListViewModel filter)
    {
        if (filter.Page < 1) filter.Page = 1;
        if (filter.PageSize < 1) filter.PageSize = 20;

        var (rows, total) = await _svc.GetSalesAsync(filter);
        filter.Rows = rows;
        filter.TotalCount = total;

        filter.TopProducts = await _svc.GetTopProductsAsync(20);
        filter.Categories = await _svc.GetCategoriesAsync();
        filter.Cities = await _svc.GetCitiesAsync();

        return View(filter);
    }
}