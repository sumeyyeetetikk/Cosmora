using System.Data;
using Cosmora.Context;
using Cosmora.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System.Data.SqlClient;

namespace Cosmora.Context.SeedData;

public static class SalesSeeder
{
    // Case referans günü: 15 Ağustos 2026. Tam 3 yıl geriye.
    private static readonly DateTime EndDate = new(2026, 8, 15);
    private static readonly DateTime StartDate = new(2023, 8, 15);

    private const int TargetRows = 1_000_000;
    private const int BatchSize = 50_000;

    // Deterministik olsun diye sabit seed — her çalıştırmada aynı veri üretilir
    private static readonly Random Rng = new(12345);

    public static void Seed(CosmoraDbContext db, string connectionString)
    {
        long existing = db.Sales.LongCount();
        if (existing >= TargetRows) return;         // tam dolu (1M) → çık
        if (existing > 0)                           // yarım kalmış (ör. 300k) → temizle, baştan
            db.Database.ExecuteSqlRaw("TRUNCATE TABLE Sales");

        // Boyutları belleğe küçük listeler olarak alıyoruz (100 ürün + 30 şehir, minik)
        var products = db.Products
            .Select(p => new ProdRef
            {
                Id = p.Id,
                BasePrice = p.BaseUnitPrice,
                Popularity = p.Popularity,
                Season = p.Seasonality
            }).ToList();

        var cities = db.Cities
            .Select(c => new CityRef { Id = c.Id, Weight = c.SalesWeight })
            .ToList();

        int totalDays = (int)(EndDate - StartDate).TotalDays + 1;

        var weightedPairs = BuildWeightedPairs(products, cities, out double totalWeight);

        var table = CreateSalesTable();
        int written = 0;

        while (written < TargetRows)
        {
            int rowsThisBatch = Math.Min(BatchSize, TargetRows - written);

            for (int i = 0; i < rowsThisBatch; i++)
            {
                var pair = PickWeighted(weightedPairs, totalWeight);
                var product = pair.Product;
                var city = pair.City;

                // 1) Rastgele tarih
                var date = StartDate.AddDays(Rng.Next(totalDays));

                // 2) Kampanya günü mü? (yaklaşık %12 gün kampanyalı)
                bool isCampaign = Rng.NextDouble() < 0.12;

                // 3) İndirim oranı: kampanyada 15-50%, normalde 0-10%
                decimal discount = isCampaign
                    ? (decimal)(0.15 + Rng.NextDouble() * 0.35)
                    : (decimal)(Rng.NextDouble() * 0.10);
                discount = Math.Round(discount, 4);

                // 4) TALEP ÇARPANLARI
                double seasonMult = SeasonMultiplier(product.Season, date);
                double weekendMult = (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) ? 1.25 : 1.0;
                double campaignMult = isCampaign ? 1.6 : 1.0;
                double discountMult = 1.0 + (double)discount * 1.2; // indirim adedi artırır
                double noise = 0.7 + Rng.NextDouble() * 0.6; // 0.7 - 1.3 arası gürültü

                double baseQty = 3.0 * product.Popularity;
                double qtyD = baseQty * seasonMult * weekendMult
                              * campaignMult * discountMult * noise;

                int quantity = Math.Max(1, (int)Math.Round(qtyD));

                // 5) AYKIRI DEĞER: ~%2 satırda anormal sıçrama/düşüş
                if (Rng.NextDouble() < 0.02)
                    quantity = Rng.NextDouble() < 0.5
                        ? quantity * (5 + Rng.Next(6))   // ani patlama
                        : Math.Max(1, quantity / 4);      // ani çöküş

                
                decimal unitPrice = Math.Round(product.BasePrice * (1 - discount), 2);
                decimal total = Math.Round(unitPrice * quantity, 2);

                var payment = (PaymentMethod)Rng.Next(0, 4);

                var row = table.NewRow();
                row["OrderDate"] = date;
                row["ProductId"] = product.Id;
                row["CityId"] = city.Id;
                row["UnitPrice"] = unitPrice;
                row["Quantity"] = quantity;
                row["TotalAmount"] = total;
                row["PaymentMethod"] = payment.ToString();
                row["DiscountRate"] = discount;
                row["IsCampaign"] = isCampaign;
                table.Rows.Add(row);
            }

            BulkInsert(table, connectionString);
            written += rowsThisBatch;
            table.Clear(); // batch'i bellekten temizle
            Console.WriteLine($"[Seeder] {written:N0}/{TargetRows:N0} satır yazıldı.");
        }
    }

    // Mevsim eğrisi: aya göre çarpan. Case'in mevsimsellik kuralını uygular.
    private static double SeasonMultiplier(SeasonalPattern season, DateTime date)
    {
        int m = date.Month;
        return season switch
        {
            // Güneş ürünleri: yaz zirvesi, kış dibi
            SeasonalPattern.Summer => m is 6 or 7 or 8 ? 3.2 : m is 5 or 9 ? 1.6 : 0.5,
            // Kış bakımı: kış zirvesi
            SeasonalPattern.Winter => m is 12 or 1 or 2 ? 2.2 : m is 11 or 3 ? 1.4 : 0.8,
            // İlkbahar
            SeasonalPattern.Spring => m is 3 or 4 or 5 ? 1.8 : 1.0,
            // Hediye zirveleri: Şubat (Sevgililer), Mayıs (Anneler), Aralık (Yılbaşı)
            SeasonalPattern.HolidayGift => m is 2 or 5 or 12 ? 2.4 : 1.0,
            _ => 1.0
        };
    }

    // Ürün x şehir çiftlerini ağırlıklarıyla önceden hesapla
    private static List<WeightedPair> BuildWeightedPairs(
        List<ProdRef> products, List<CityRef> cities, out double totalWeight)
    {
        var pairs = new List<WeightedPair>(products.Count * cities.Count);
        double sum = 0;
        foreach (var p in products)
            foreach (var c in cities)
            {
                double w = p.Popularity * c.Weight;
                sum += w;
                pairs.Add(new WeightedPair { Product = p, City = c, Weight = w, CumWeight = sum });
            }
        totalWeight = sum;
        return pairs;
    }

    // Ağırlığa göre bir çift seç (binary search ile hızlı)
    private static WeightedPair PickWeighted(List<WeightedPair> pairs, double totalWeight)
    {
        double r = Rng.NextDouble() * totalWeight;
        int lo = 0, hi = pairs.Count - 1;
        while (lo < hi)
        {
            int mid = (lo + hi) / 2;
            if (pairs[mid].CumWeight < r) lo = mid + 1;
            else hi = mid;
        }
        return pairs[lo];
    }

    private static DataTable CreateSalesTable()
    {
        var t = new DataTable();
        t.Columns.Add("OrderDate", typeof(DateTime));
        t.Columns.Add("ProductId", typeof(int));
        t.Columns.Add("CityId", typeof(int));
        t.Columns.Add("UnitPrice", typeof(decimal));
        t.Columns.Add("Quantity", typeof(int));
        t.Columns.Add("TotalAmount", typeof(decimal));
        t.Columns.Add("PaymentMethod", typeof(string));
        t.Columns.Add("DiscountRate", typeof(decimal));
        t.Columns.Add("IsCampaign", typeof(bool));
        return t;
    }

    private static void BulkInsert(DataTable table, string connectionString)
    {
        using var conn = new SqlConnection(connectionString);
        conn.Open();
        using var bulk = new SqlBulkCopy(conn)
        {
            DestinationTableName = "Sales",
            BatchSize = BatchSize,
            BulkCopyTimeout = 0
        };

        foreach (DataColumn col in table.Columns)
            bulk.ColumnMappings.Add(col.ColumnName, col.ColumnName);

        bulk.WriteToServer(table);
    }

    private class ProdRef
    {
        public int Id; public decimal BasePrice; public double Popularity; public SeasonalPattern Season;
    }
    private class CityRef { public int Id; public double Weight; }
    private class WeightedPair
    {
        public ProdRef Product = null!; public CityRef City = null!;
        public double Weight; public double CumWeight;
    }
}