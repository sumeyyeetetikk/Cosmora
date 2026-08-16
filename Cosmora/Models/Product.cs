using Cosmora.Models.Enums;

namespace Cosmora.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;

        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public decimal BaseUnitPrice { get; set; }

        public double Popularity { get; set; }

        public SeasonalPattern Seasonality { get; set; }

        public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    }
}
