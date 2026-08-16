using Cosmora.Context;
using Cosmora.Models;
using Cosmora.Models.Enums;

namespace Cosmora.Context.SeedData;

public static class DimensionSeeder
{
    public static void Seed(CosmoraDbContext db)
    {
        if (db.Categories.Any()) return; 

        var categories = new List<Category>
        {
            new() { Name = "Makyaj" },
            new() { Name = "Cilt Bakımı" },
            new() { Name = "Saç Bakımı" },
            new() { Name = "Parfüm & Deodorant" },
            new() { Name = "Kişisel Bakım" },
            new() { Name = "Güneş Ürünleri" },
            new() { Name = "Erkek Bakım" },
            new() { Name = "Anne & Bebek" },
        };
        db.Categories.AddRange(categories);
        db.SaveChanges();

        var cat = categories.ToDictionary(c => c.Name, c => c.Id);

        // SalesWeight: metropoller yüksek, küçük şehirler düşük 
        var cities = new List<City>
        {
            // Türkiye
            new() { Name = "İstanbul", Country = "Türkiye", SalesWeight = 3.0 },
            new() { Name = "Ankara",   Country = "Türkiye", SalesWeight = 1.8 },
            new() { Name = "İzmir",    Country = "Türkiye", SalesWeight = 1.5 },
            // Almanya
            new() { Name = "Berlin",   Country = "Almanya", SalesWeight = 2.6 },
            new() { Name = "Münih",    Country = "Almanya", SalesWeight = 1.7 },
            new() { Name = "Hamburg",  Country = "Almanya", SalesWeight = 1.4 },
            // Fransa
            new() { Name = "Paris",    Country = "Fransa",  SalesWeight = 2.8 },
            new() { Name = "Lyon",     Country = "Fransa",  SalesWeight = 1.3 },
            new() { Name = "Marsilya", Country = "Fransa",  SalesWeight = 1.2 },
            // İngiltere
            new() { Name = "Londra",   Country = "İngiltere", SalesWeight = 2.9 },
            new() { Name = "Manchester", Country = "İngiltere", SalesWeight = 1.4 },
            new() { Name = "Birmingham", Country = "İngiltere", SalesWeight = 1.1 },
            // İtalya
            new() { Name = "Roma",     Country = "İtalya",  SalesWeight = 2.2 },
            new() { Name = "Milano",   Country = "İtalya",  SalesWeight = 2.0 },
            new() { Name = "Napoli",   Country = "İtalya",  SalesWeight = 1.0 },
            // İspanya
            new() { Name = "Madrid",   Country = "İspanya", SalesWeight = 2.3 },
            new() { Name = "Barselona",Country = "İspanya", SalesWeight = 2.1 },
            new() { Name = "Sevilla",  Country = "İspanya", SalesWeight = 0.9 },
            // Hollanda
            new() { Name = "Amsterdam",Country = "Hollanda", SalesWeight = 1.9 },
            new() { Name = "Rotterdam",Country = "Hollanda", SalesWeight = 1.1 },
            new() { Name = "Lahey",    Country = "Hollanda", SalesWeight = 0.8 },
            // Polonya
            new() { Name = "Varşova",  Country = "Polonya", SalesWeight = 1.6 },
            new() { Name = "Krakov",   Country = "Polonya", SalesWeight = 1.0 },
            new() { Name = "Gdansk",   Country = "Polonya", SalesWeight = 0.7 },
            // İsveç
            new() { Name = "Stockholm",Country = "İsveç",   SalesWeight = 1.5 },
            new() { Name = "Göteborg", Country = "İsveç",   SalesWeight = 0.9 },
            new() { Name = "Malmö",    Country = "İsveç",   SalesWeight = 0.6 },
            // Avusturya
            new() { Name = "Viyana",   Country = "Avusturya", SalesWeight = 1.7 },
            new() { Name = "Graz",     Country = "Avusturya", SalesWeight = 0.8 },
            new() { Name = "Linz",     Country = "Avusturya", SalesWeight = 0.6 },
        };
        db.Cities.AddRange(cities);
        db.SaveChanges();

        // --- 100 ÜRÜN ---
        db.Products.AddRange(BuildProducts(cat));
        db.SaveChanges();
    }

    // Her kategoriye ürünler dağıtılır. Popularity: 0.2 (niş) .. 3.0 (çok satan).
    // Seasonality: seeder'ın mevsim eğrisini seçmesini sağlar.
    private static List<Product> BuildProducts(Dictionary<string, int> cat)
    {
        var p = new List<Product>();

        // Makyaj (HolidayGift ağırlıklı) — 16 ürün
        AddRange(p, cat["Makyaj"], SeasonalPattern.HolidayGift, new (string, decimal, double)[]
        {
            ("Mat Ruj - Kırmızı", 189, 3.0), ("Mat Ruj - Nude", 189, 2.4),
            ("Likit Fondöten", 259, 2.6), ("Kapatıcı Stick", 149, 1.8),
            ("Maskara Volüm", 199, 2.7), ("Eyeliner Kalem", 129, 1.6),
            ("Allık Paleti", 219, 1.4), ("Far Paleti 12'li", 349, 1.9),
            ("Ruj Seti 3'lü", 399, 1.2), ("Pudra Kompakt", 179, 1.7),
            ("Kaş Maskarası", 119, 1.1), ("Highlighter", 209, 1.0),
            ("Lip Gloss", 139, 1.5), ("Oje - Kırmızı", 89, 1.3),
            ("Makyaj Fırça Seti", 299, 0.9), ("BB Krem", 169, 1.6),
        });

        // Cilt Bakımı (None/Winter karışık) — 16 ürün
        AddRange(p, cat["Cilt Bakımı"], SeasonalPattern.Winter, new (string, decimal, double)[]
        {
            ("Hyaluronik Nem Serumu", 289, 3.0), ("Günlük Nemlendirici", 199, 2.8),
            ("Yoğun Bakım Kremi", 249, 2.2), ("C Vitamini Serum", 319, 2.1),
            ("Retinol Gece Kremi", 359, 1.6), ("Göz Kremi", 229, 1.7),
            ("Peeling Jel", 159, 1.4), ("Kil Maske", 139, 1.5),
            ("Yüz Temizleme Jeli", 129, 2.5), ("Tonik", 149, 1.8),
            ("Micellar Su", 119, 2.0), ("Yüz Serumu Niasinamid", 279, 1.9),
            ("Dudak Balsamı", 69, 2.3), ("El Kremi", 79, 2.1),
            ("Yağ Bazlı Temizleyici", 169, 1.2), ("Nemlendirici Maske", 99, 1.3),
        });

        // Saç Bakımı — 14 ürün
        AddRange(p, cat["Saç Bakımı"], SeasonalPattern.None, new (string, decimal, double)[]
        {
            ("Onarıcı Şampuan", 149, 2.9), ("Günlük Şampuan", 119, 2.7),
            ("Saç Kremi", 129, 2.4), ("Saç Maskesi", 179, 1.8),
            ("Saç Serumu", 199, 1.6), ("Kuru Şampuan", 139, 1.5),
            ("Isı Koruyucu Sprey", 159, 1.3), ("Saç Köpüğü", 109, 1.0),
            ("Saç Spreyi", 99, 1.1), ("Boya - Kahve", 189, 1.2),
            ("Kepek Şampuanı", 149, 1.7), ("Argan Yağı", 219, 1.4),
            ("Saç Kremi Sülfatsız", 169, 1.3), ("Tuz Spreyi", 129, 0.8),
        });

        // Parfüm & Deodorant (HolidayGift) — 12 ürün
        AddRange(p, cat["Parfüm & Deodorant"], SeasonalPattern.HolidayGift, new (string, decimal, double)[]
        {
            ("Kadın Parfüm 50ml", 899, 2.2), ("Erkek Parfüm 100ml", 999, 2.0),
            ("Unisex Parfüm", 799, 1.4), ("Roll-on Deodorant", 79, 2.6),
            ("Sprey Deodorant", 99, 2.5), ("Parfüm Seti", 1299, 0.9),
            ("Mini Parfüm 10ml", 249, 1.6), ("Vücut Spreyi", 129, 1.8),
            ("Kadın Parfüm 30ml", 599, 1.7), ("Erkek Parfüm 50ml", 699, 1.5),
            ("Kolonya", 89, 1.3), ("Katı Parfüm", 159, 0.7),
        });

        // Kişisel Bakım — 12 ürün
        AddRange(p, cat["Kişisel Bakım"], SeasonalPattern.None, new (string, decimal, double)[]
        {
            ("Duş Jeli", 79, 3.0), ("Sıvı Sabun", 59, 2.8),
            ("Diş Macunu", 49, 2.9), ("Diş Fırçası", 39, 2.4),
            ("Vücut Losyonu", 119, 2.1), ("Ağız Gargarası", 89, 1.5),
            ("Islak Mendil", 45, 2.2), ("Tıraş Jeli", 99, 1.6),
            ("Ped Günlük", 69, 1.9), ("Pamuk Çubuk", 29, 1.4),
            ("Vücut Peelingi", 139, 1.1), ("Ayak Kremi", 89, 1.0),
        });

        // Güneş Ürünleri (Summer) — 10 ürün
        AddRange(p, cat["Güneş Ürünleri"], SeasonalPattern.Summer, new (string, decimal, double)[]
        {
            ("SPF50 Güneş Kremi", 249, 2.8), ("SPF30 Güneş Sütü", 199, 2.3),
            ("Güneş Sonrası Jel", 149, 1.7), ("Yüz Güneş Kremi SPF50", 279, 2.0),
            ("Bronzlaştırıcı Yağ", 169, 1.3), ("Çocuk Güneş Kremi", 219, 1.5),
            ("Güneş Spreyi SPF30", 189, 1.4), ("Renkli Güneş Kremi", 299, 1.1),
            ("Dudak Güneş Koruyucu", 79, 0.9), ("Su Bazlı Güneş Kremi", 259, 1.2),
        });

        // Erkek Bakım — 10 ürün
        AddRange(p, cat["Erkek Bakım"], SeasonalPattern.None, new (string, decimal, double)[]
        {
            ("Tıraş Köpüğü", 89, 2.5), ("Tıraş Sonrası Balsam", 129, 2.0),
            ("Sakal Yağı", 159, 1.6), ("Sakal Şampuanı", 119, 1.3),
            ("Erkek Yüz Kremi", 149, 1.7), ("Tıraş Jeli Hassas", 99, 1.4),
            ("Sakal Düzenleyici Krem", 139, 1.1), ("Erkek Duş Jeli", 99, 2.1),
            ("Saç & Sakal Şekillendirici", 129, 1.2), ("Tıraş Bıçağı 4'lü", 179, 1.5),
        });

        // Anne & Bebek — 10 ürün
        AddRange(p, cat["Anne & Bebek"], SeasonalPattern.None, new (string, decimal, double)[]
        {
            ("Bebek Bezi Midi", 299, 2.9), ("Bebek Islak Mendil", 79, 2.7),
            ("Bebek Şampuanı", 119, 2.2), ("Pişik Kremi", 99, 1.8),
            ("Bebek Losyonu", 109, 1.6), ("Bebek Güneş Kremi", 199, 1.2),
            ("Emzik", 89, 1.0), ("Biberon", 149, 1.1),
            ("Bebek Bezi Maxi", 319, 2.4), ("Bebek Yağı", 89, 1.3),
        });

        return p;
    }

    private static void AddRange(List<Product> list, int categoryId,
        SeasonalPattern season, (string name, decimal price, double pop)[] items)
    {
        foreach (var (name, price, pop) in items)
            list.Add(new Product
            {
                Name = name,
                CategoryId = categoryId,
                BaseUnitPrice = price,
                Popularity = pop,
                Seasonality = season
            });
    }
}