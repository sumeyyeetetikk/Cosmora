using Cosmora.Models.ViewModels;
using Cosmora.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cosmora.Controllers;

public class AiAnalysisController : Controller
{
    private readonly IAiAnalysisService _svc;
    public AiAnalysisController(IAiAnalysisService svc) => _svc = svc;

    public IActionResult Index() => View(new AiAnalysisViewModel());

    [HttpPost]
    public async Task<IActionResult> Analyze()
    {
        var vm = await _svc.AnalyzeAsync();
        return View("Index", vm);
    }
}