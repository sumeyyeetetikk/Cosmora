using Cosmora.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cosmora.Controllers;

public class DashboardController : Controller
{
    private readonly IDashboardService _dashboard;
    public DashboardController(IDashboardService dashboard) => _dashboard = dashboard;

    public async Task<IActionResult> Index()
    {
        var vm = await _dashboard.GetDashboardAsync();
        return View(vm);
    }
}