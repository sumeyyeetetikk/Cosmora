namespace Cosmora.Models.ViewModels;

public class SalesChatViewModel
{
    public string? Question { get; set; }
    public string? Answer { get; set; }
    public string? Error { get; set; }

    // Şeffaflık: hangi veriyi çektik, LLM ne niyet çıkardı
    public string? IntentJson { get; set; }
    public string? DataResult { get; set; }
    public bool HasResult { get; set; }
}