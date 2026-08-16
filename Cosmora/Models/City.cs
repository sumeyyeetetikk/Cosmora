namespace Cosmora.Models
{
    public class City
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Country { get; set; } = null!;

        public double SalesWeight { get; set; }

        public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    }
}
