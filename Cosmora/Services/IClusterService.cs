using Cosmora.Models.ViewModels;

namespace Cosmora.Services
{
    public interface IClusterService
    {
        Task<ClusterViewModel> RunAsync();
    }
}
