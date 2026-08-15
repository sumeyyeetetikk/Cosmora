using Cosmora.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cosmora.Controllers;

public class AnomalyController : Controller
{
    private readonly IAnomalyService _svc;
    public AnomalyController(IAnomalyService svc) => _svc = svc;

    public async Task<IActionResult> Index(int? cityId)
    {
        var cities = await _svc.GetCitiesAsync();
        int selected = cityId ?? cities.FirstOrDefault()?.Id ?? 0;

        var vm = await _svc.DetectAsync(selected);
        vm.Cities = cities;
        return View(vm);
    }
}