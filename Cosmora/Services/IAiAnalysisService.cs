using Cosmora.Models.ViewModels;

namespace Cosmora.Services
{
    public interface IAiAnalysisService
    {
        Task<AiAnalysisViewModel> AnalyzeAsync();
    }
}
