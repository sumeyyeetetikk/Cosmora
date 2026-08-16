namespace Cosmora.Models.ViewModels;

public class AiAnalysisViewModel
{
    public bool HasResult { get; set; }
    public string? AnalysisText { get; set; }   
    public string? Error { get; set; }

    // LLM'e gönderilen özet veriyi ekranda da gösterelim (şeffaflık)
    public string DataContext { get; set; } = "";
    public DateTime GeneratedAt { get; set; }
}