using Cosmora.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cosmora.Controllers;

public class ForecastController : Controller
{
    private readonly IForecastService _svc;
    public ForecastController(IForecastService svc) => _svc = svc;

    public async Task<IActionResult> Index(int? cityId)
    {
        // cityId gelmezse ilk şehri varsayılan seç
        var cities = await _svc.GetCitiesAsync();
        int selected = cityId ?? cities.FirstOrDefault()?.Id ?? 0;

        var vm = await _svc.ForecastAsync(selected);
        vm.Cities = cities;
        return View(vm);
    }
}