using Cosmora.Models.ViewModels;

namespace Cosmora.Services
{
    public interface IProductService
    {
        Task<List<TopProductRow>> GetTopProductsAsync(int take = 20);
        Task<(List<SaleRow> rows, int totalCount)> GetSalesAsync(SalesListViewModel filter);
        Task<List<FilterOption>> GetCategoriesAsync();
        Task<List<FilterOption>> GetCitiesAsync();
    }
}
