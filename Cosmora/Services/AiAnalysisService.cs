using System.Text;
using System.Text.Json;
using Cosmora.Context;
using Cosmora.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Cosmora.Services;

public class AiAnalysisService : IAiAnalysisService
{
    private readonly CosmoraDbContext _db;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;

    public AiAnalysisService(CosmoraDbContext db, IHttpClientFactory httpFactory, IConfiguration config)
    {
        _db = db;
        _httpFactory = httpFactory;
        _config = config;
    }

    public async Task<AiAnalysisViewModel> AnalyzeAsync()
    {
        var vm = new AiAnalysisViewModel { GeneratedAt = DateTime.Now };

        // 1) GERÇEK VERİYİ TOPLA — hepsi SQL'de aggregate
        // "Bugün" = verinin son günü
        var maxDate = await _db.Sales.MaxAsync(s => (DateTime?)s.OrderDate) ?? DateTime.Now;
        var last30Start = maxDate.AddDays(-30);

        // Son 30 gün toplamları
        var last30 = await _db.Sales
            .Where(s => s.OrderDate >= last30Start)
            .GroupBy(s => 1)
            .Select(g => new
            {
                Revenue = g.Sum(x => x.TotalAmount),
                Qty = g.Sum(x => (long)x.Quantity),
                Orders = g.Count()
            })
            .FirstOrDefaultAsync();

        // Önceki 30 gün (trend karşılaştırması için)
        var prev30 = await _db.Sales
            .Where(s => s.OrderDate >= last30Start.AddDays(-30) && s.OrderDate < last30Start)
            .SumAsync(s => (decimal?)s.TotalAmount) ?? 0m;

        // Son 30 günde en çok satan 5 ürün
        var topProducts = await _db.Sales
            .Where(s => s.OrderDate >= last30Start)
            .GroupBy(s => s.Product.Name)
            .Select(g => new { Name = g.Key, Qty = g.Sum(x => (long)x.Quantity) })
            .OrderByDescending(x => x.Qty)
            .Take(5)
            .ToListAsync();

        // Son 30 günde en çok ciro yapan 5 şehir
        var topCities = await _db.Sales
            .Where(s => s.OrderDate >= last30Start)
            .GroupBy(s => s.City.Name)
            .Select(g => new { Name = g.Key, Revenue = g.Sum(x => x.TotalAmount) })
            .OrderByDescending(x => x.Revenue)
            .Take(5)
            .ToListAsync();

        // Kategori bazlı son 30 gün cirosu
        var byCategory = await _db.Sales
            .Where(s => s.OrderDate >= last30Start)
            .GroupBy(s => s.Product.Category.Name)
            .Select(g => new { Name = g.Key, Revenue = g.Sum(x => x.TotalAmount) })
            .OrderByDescending(x => x.Revenue)
            .ToListAsync();

        int nextMonth = maxDate.AddMonths(1).Month;
        string seasonHint = nextMonth switch
        {
            6 or 7 or 8 => "yaz (güneş ürünleri talebi zirvede)",
            12 or 1 => "kış (nemlendirici/dudak bakımı talebi yüksek)",
            2 => "Şubat — kış bakımı + Sevgililer Günü (parfüm & makyaj)",
            5 => "Anneler Günü dönemi (hediye kategorileri)",
            _ => "geçiş dönemi"
        };

        // 2) VERİYİ METNE DÖK (LLM'e bağlam olarak)
        var revenue = last30?.Revenue ?? 0;
        var trendPct = prev30 > 0 ? ((double)(revenue - prev30) / (double)prev30) * 100 : 0;

        var sb = new StringBuilder();
        sb.AppendLine($"Cosmora kozmetik e-ticaret verisi (referans gün: {maxDate:dd.MM.yyyy}).");
        sb.AppendLine();
        sb.AppendLine($"SON 30 GÜN:");
        sb.AppendLine($"- Toplam ciro: {revenue:N0} TL");
        sb.AppendLine($"- Toplam adet: {last30?.Qty ?? 0:N0}");
        sb.AppendLine($"- Sipariş sayısı: {last30?.Orders ?? 0:N0}");
        sb.AppendLine($"- Önceki 30 güne göre ciro değişimi: %{trendPct:N1}");
        sb.AppendLine();
        sb.AppendLine("EN ÇOK SATAN 5 ÜRÜN (adet):");
        foreach (var p in topProducts) sb.AppendLine($"- {p.Name}: {p.Qty:N0}");
        sb.AppendLine();
        sb.AppendLine("EN ÇOK CİRO YAPAN 5 ŞEHİR:");
        foreach (var c in topCities) sb.AppendLine($"- {c.Name}: {c.Revenue:N0} TL");
        sb.AppendLine();
        sb.AppendLine("KATEGORİ CİROLARI:");
        foreach (var c in byCategory) sb.AppendLine($"- {c.Name}: {c.Revenue:N0} TL");
        sb.AppendLine();
        sb.AppendLine($"YAKLAŞAN DÖNEM: Gelecek ay {nextMonth}. ay → {seasonHint}.");

        vm.DataContext = sb.ToString();

        // 3) GROQ'A GÖNDER
        var apiKey = _config["Groq:ApiKey"];
        var model = _config["Groq:Model"] ?? "llama-3.3-70b-versatile";

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "BURAYA_GROQ_KEY")
        {
            vm.Error = "Groq API key ayarlanmamış. appsettings.json içindeki Groq:ApiKey alanını doldurun.";
            return vm;
        }

        var systemPrompt =
            "Sen bir kozmetik e-ticaret şirketinin kıdemli satış analistisin. " +
            "Sana verilen GERÇEK satış verilerini yorumla. SADECE verilen verilere dayan, " +
            "veri uydurma. Yanıtını Türkçe, 3 başlıkta ver: " +
            "1) GENEL DURUM (2-3 cümle özet ve trend yorumu), " +
            "2) DİKKAT ÇEKENLER (öne çıkan ürün/şehir/kategori gözlemleri), " +
            "3) AKSİYON ÖNERİLERİ (yaklaşan sezona göre 3-4 somut, uygulanabilir öneri). " +
            "Kısa ve yönetici-dostu yaz, madde işaretleri kullanabilirsin.";

        try
        {
            var requestBody = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = vm.DataContext }
                },
                temperature = 0.5,
                max_tokens = 900
            };

            var http = _httpFactory.CreateClient();
            http.Timeout = TimeSpan.FromSeconds(30);

            using var req = new HttpRequestMessage(HttpMethod.Post,
                "https://api.groq.com/openai/v1/chat/completions");
            req.Headers.Add("Authorization", $"Bearer {apiKey}");
            req.Content = new StringContent(
                JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var resp = await http.SendAsync(req);
            var json = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                vm.Error = $"Groq API hatası ({(int)resp.StatusCode}): {json}";
                return vm;
            }

            using var doc = JsonDocument.Parse(json);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            vm.AnalysisText = content?.Trim();
            vm.HasResult = !string.IsNullOrWhiteSpace(vm.AnalysisText);
        }
        catch (Exception ex)
        {
            vm.Error = "Analiz sırasında hata: " + ex.Message;
        }

        return vm;
    }
}