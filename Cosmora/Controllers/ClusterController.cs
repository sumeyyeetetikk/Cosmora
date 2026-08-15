using Cosmora.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cosmora.Controllers;

public class ClusterController : Controller
{
    private readonly IClusterService _svc;
    public ClusterController(IClusterService svc) => _svc = svc;

    public async Task<IActionResult> Index()
    {
        var vm = await _svc.RunAsync();
        return View(vm);
    }
}