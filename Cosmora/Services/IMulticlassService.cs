using Cosmora.Models.ViewModels;

namespace Cosmora.Services
{
    public interface IMulticlassService
    {
        Task<MulticlassViewModel> RunAsync();
    }
}
