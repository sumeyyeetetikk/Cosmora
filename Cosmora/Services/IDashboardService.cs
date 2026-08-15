using Cosmora.Models.ViewModels;

namespace Cosmora.Services
{
    public interface IDashboardService
    {

        Task<DashboardViewModel> GetDashboardAsync();
    }
}
