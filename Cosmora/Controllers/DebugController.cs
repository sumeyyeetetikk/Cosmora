using Microsoft.AspNetCore.Mvc;

namespace Cosmora.Controllers;

public class DebugController : Controller
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;

    public DebugController(IHttpClientFactory httpFactory, IConfiguration config)
    {
        _httpFactory = httpFactory;
        _config = config;
    }

    // /Debug/Models -> Cerebras'taki modelleri ham JSON olarak gösterir
    public async Task<IActionResult> Models()
    {
        var apiKey = _config["Gemini:ApiKey"];
        var http = _httpFactory.CreateClient();
        using var req = new HttpRequestMessage(HttpMethod.Get,
            "https://generativelanguage.googleapis.com/v1beta/openai/models");
        req.Headers.Add("Authorization", $"Bearer {apiKey}");
        var resp = await http.SendAsync(req);
        var json = await resp.Content.ReadAsStringAsync();
        return Content(json, "application/json");
    }
}