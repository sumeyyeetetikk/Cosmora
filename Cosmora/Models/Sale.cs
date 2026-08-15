using Cosmora.Models.Enums;

namespace Cosmora.Models
{
    public class Sale
    {
        public long Id { get; set; }              // 1M+ kayıt için long
        public DateTime OrderDate { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int CityId { get; set; }
        public City City { get; set; } = null!;

        public decimal UnitPrice { get; set; }    // satış anındaki fiyat (indirimli olabilir)
        public int Quantity { get; set; }         // ML hedeflerinin çoğu bunun üzerinden
        public decimal TotalAmount { get; set; }  // UnitPrice * Quantity * (1 - DiscountRate)

        public PaymentMethod PaymentMethod { get; set; }
        public decimal DiscountRate { get; set; }
        public bool IsCampaign { get; set; }
    }
}
