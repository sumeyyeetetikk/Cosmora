namespace Cosmora.Models
{
    public class City
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Country { get; set; } = null!;

        // Göreli satış ağırlığı: metropol > küçük şehir (clustering'i anlamlı kılar)
        public double SalesWeight { get; set; }

        public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    }
}
