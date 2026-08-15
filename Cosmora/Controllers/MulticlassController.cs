using Cosmora.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cosmora.Controllers;

public class MulticlassController : Controller
{
    private readonly IMulticlassService _svc;
    public MulticlassController(IMulticlassService svc) => _svc = svc;

    public async Task<IActionResult> Index()
    {
        var vm = await _svc.RunAsync();
        return View(vm);
    }
}