using Cosmora.Models.ViewModels;

namespace Cosmora.Services
{
    public interface ISalesChatService
    {
        Task<SalesChatViewModel> AskAsync(string question);
    }
}
