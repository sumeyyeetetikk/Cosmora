using Cosmora.Models.ViewModels;
using Cosmora.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cosmora.Controllers;

public class AiAnalysisController : Controller
{
    private readonly IAiAnalysisService _svc;
    public AiAnalysisController(IAiAnalysisService svc) => _svc = svc;

    // İlk açılış: boş ekran (buton bekliyor)
    public IActionResult Index() => View(new AiAnalysisViewModel());

    // Butona basınca: analiz üret
    [HttpPost]
    public async Task<IActionResult> Analyze()
    {
        var vm = await _svc.AnalyzeAsync();
        return View("Index", vm);
    }
}