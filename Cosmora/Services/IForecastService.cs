using Cosmora.Models.ViewModels;

namespace Cosmora.Services
{
    public interface IForecastService
    {
        Task<List<FilterOption>> GetCitiesAsync();
        Task<ForecastViewModel> ForecastAsync(int cityId);
    }
}
