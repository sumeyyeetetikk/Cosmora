using Cosmora.Models.ViewModels;
using Cosmora.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cosmora.Controllers;

public class SalesChatController : Controller
{
    private readonly ISalesChatService _svc;
    public SalesChatController(ISalesChatService svc) => _svc = svc;

    public IActionResult Index() => View(new SalesChatViewModel());

    [HttpPost]
    public async Task<IActionResult> Ask(string question)
    {
        var vm = await _svc.AskAsync(question);
        return View("Index", vm);
    }
}