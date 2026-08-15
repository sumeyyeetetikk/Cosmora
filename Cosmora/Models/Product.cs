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

        // Göreli satış hacmi çarpanı (seeder kullanır): 0.2 = niş, 3.0 = çok satan
        public double Popularity { get; set; }

        // Mevsimsel talep desenini belirler
        public SeasonalPattern Seasonality { get; set; }

        public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    }
}
