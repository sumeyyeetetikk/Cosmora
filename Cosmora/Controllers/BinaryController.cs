using Cosmora.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cosmora.Controllers;

public class BinaryController : Controller
{
    private readonly IBinaryClassificationService _svc;
    public BinaryController(IBinaryClassificationService svc) => _svc = svc;

    public async Task<IActionResult> Index()
    {
        var vm = await _svc.RunAsync();
        return View(vm);
    }
}