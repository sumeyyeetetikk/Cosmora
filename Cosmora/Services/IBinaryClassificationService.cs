using Cosmora.Models.ViewModels;

namespace Cosmora.Services
{
    public interface IBinaryClassificationService
    {
        Task<BinaryViewModel> RunAsync();
    }
}
