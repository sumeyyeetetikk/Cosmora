using Cosmora.Models.ViewModels;

namespace Cosmora.Services
{
    public interface IAnomalyService
    {
        Task<List<FilterOption>> GetCitiesAsync();
        Task<AnomalyViewModel> DetectAsync(int cityId);
    }
}
