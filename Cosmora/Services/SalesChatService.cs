using System.Text;
using System.Text.Json;
using Cosmora.Context;
using Cosmora.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Cosmora.Services;

public class SalesChatService : ISalesChatService
{
    private readonly CosmoraDbContext _db;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;

    public SalesChatService(CosmoraDbContext db, IHttpClientFactory httpFactory, IConfiguration config)
    {
        _db = db;
        _httpFactory = httpFactory;
        _config = config;
    }

    public async Task<SalesChatViewModel> AskAsync(string question)
    {
        var vm = new SalesChatViewModel { Question = question };

        if (string.IsNullOrWhiteSpace(question))
        {
            vm.Error = "Lütfen bir soru yazın.";
            return vm;
        }

        var apiKey = _config["Gemini:ApiKey"];
        var model = _config["Gemini:Model"] ?? "gemini-2.0-flash";
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "BURAYA_GEMINI_KEY")
        {
            vm.Error = "Gemini API key ayarlanmamış. appsettings.json içindeki Gemini:ApiKey alanını doldurun.";
            return vm;
        }

        // 1) LLM'DEN NİYET (JSON) AL — ham SQL değil, sınırlı bir şema
        string intentSystem =
            "Kullanıcının kozmetik satış verisi hakkındaki sorusunu analiz et ve SADECE " +
            "aşağıdaki JSON şemasında yanıt ver (başka hiçbir metin yazma):\n" +
            "{\n" +
            "  \"metric\": \"quantity\" | \"revenue\" | \"orders\",\n" +
            "  \"dimension\": \"product\" | \"category\" | \"city\" | \"country\" | \"payment\" | \"month\" | \"none\",\n" +
            "  \"filterCity\": null veya şehir adı,\n" +
            "  \"filterCategory\": null veya kategori adı,\n" +
            "  \"campaignOnly\": true | false,\n" +
            "  \"top\": 1-20 arası sayı (kaç sonuç),\n" +
            "  \"order\": \"desc\" | \"asc\"\n" +
            "}\n" +
            "Örnek: 'İzmir'de en çok satan 3 ürün' -> " +
            "{\"metric\":\"quantity\",\"dimension\":\"product\",\"filterCity\":\"İzmir\"," +
            "\"filterCategory\":null,\"campaignOnly\":false,\"top\":3,\"order\":\"desc\"}";

        string intentJson;
        try
        {
            intentJson = await CallLlm(apiKey, model, intentSystem, question, 300, 0.1);
            intentJson = CleanJson(intentJson);
            vm.IntentJson = intentJson;
        }
        catch (Exception ex)
        {
            vm.Error = "Niyet çıkarılamadı: " + ex.Message;
            return vm;
        }

        // 2) NİYETİ GÜVENLE PARÇALA
        QueryIntent intent;
        try
        {
            intent = JsonSerializer.Deserialize<QueryIntent>(intentJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }
        catch
        {
            vm.Error = "Model geçerli bir sorgu niyeti üretemedi. Soruyu biraz daha net yazın.";
            return vm;
        }

        // 3) NİYETE GÖRE GÜVENLİ EF CORE SORGUSU (ham SQL YOK)
        string dataText;
        try
        {
            dataText = await RunSafeQuery(intent);
            vm.DataResult = dataText;
        }
        catch (Exception ex)
        {
            vm.Error = "Sorgu çalıştırılamadı: " + ex.Message;
            return vm;
        }

        // 4) VERİYİ LLM'E VER, DOĞAL DİL CEVAP ÜRET
        string answerSystem =
            "Sen bir satış analistisin. Kullanıcının sorusunu, sana verilen GERÇEK sorgu " +
            "sonucuna dayanarak Türkçe ve kısa yanıtla. Veriyi yorumla, uydurma. " +
            "Sonuçları okunabilir cümleye dök.";
        string answerUser = $"Soru: {question}\n\nSorgu sonucu:\n{dataText}";

        try
        {
            vm.Answer = await CallLlm(apiKey, model, answerSystem, answerUser, 600, 0.4);
            vm.HasResult = true;
        }
        catch (Exception ex)
        {
            vm.Error = "Cevap üretilemedi: " + ex.Message;
        }

        return vm;
    }

    // --- Güvenli sorgu: sadece Sales üzerinde, sadece aggregate ---
    private async Task<string> RunSafeQuery(QueryIntent i)
    {
        IQueryable<Models.Sale> q = _db.Sales;

        // Filtreler
        if (!string.IsNullOrWhiteSpace(i.FilterCity))
            q = q.Where(s => s.City.Name == i.FilterCity);
        if (!string.IsNullOrWhiteSpace(i.FilterCategory))
            q = q.Where(s => s.Product.Category.Name == i.FilterCategory);
        if (i.CampaignOnly)
            q = q.Where(s => s.IsCampaign);

        int take = Math.Clamp(i.Top <= 0 ? 5 : i.Top, 1, 20);
        bool desc = (i.Order ?? "desc").ToLower() != "asc";

        string metricName = i.Metric?.ToLower() switch
        {
            "revenue" => "Ciro (TL)",
            "orders" => "Sipariş",
            _ => "Adet"
        };

        // Boyut yoksa tek toplam
        if (string.IsNullOrWhiteSpace(i.Dimension) || i.Dimension.ToLower() == "none")
        {
            var totalQty = await q.SumAsync(x => (long)x.Quantity);
            var totalRev = await q.SumAsync(x => x.TotalAmount);
            var totalOrd = await q.CountAsync();
            return $"Toplam adet: {totalQty:N0}\nToplam ciro: {totalRev:N0} TL\nSipariş: {totalOrd:N0}";
        }

        // Boyuta göre grupla
        IQueryable<IGrouping<string, Models.Sale>> grouped = i.Dimension.ToLower() switch
        {
            "product" => q.GroupBy(s => s.Product.Name),
            "category" => q.GroupBy(s => s.Product.Category.Name),
            "city" => q.GroupBy(s => s.City.Name),
            "country" => q.GroupBy(s => s.City.Country),
            "payment" => q.GroupBy(s => s.PaymentMethod.ToString()),
            "month" => q.GroupBy(s => s.OrderDate.Year + "-" + s.OrderDate.Month),
            _ => q.GroupBy(s => s.Product.Name)
        };

        // Aggregate — SQL tarafında
        var rows = await grouped.Select(g => new
        {
            Key = g.Key,
            Qty = g.Sum(x => (long)x.Quantity),
            Revenue = g.Sum(x => x.TotalAmount),
            Orders = g.Count()
        }).ToListAsync();

        // Metrik değerini seç ve sırala (bellekte, küçük sonuç seti)
        var valued = i.Metric?.ToLower() switch
        {
            "revenue" => rows.Select(r => (r.Key, Val: (decimal)r.Revenue)),
            "orders" => rows.Select(r => (r.Key, Val: (decimal)r.Orders)),
            _ => rows.Select(r => (r.Key, Val: (decimal)r.Qty))
        };

        valued = desc ? valued.OrderByDescending(x => x.Val) : valued.OrderBy(x => x.Val);
        var top = valued.Take(take).ToList();

        if (top.Count == 0) return "Bu kriterlere uyan kayıt bulunamadı.";

        var sb = new StringBuilder();
        sb.AppendLine($"{metricName} bazında sonuçlar:");
        int rank = 1;
        foreach (var (key, val) in top)
            sb.AppendLine($"{rank++}. {key}: {val:N0}");
        return sb.ToString();
    }

    // --- LLM çağrısı (OpenAI-uyumlu; Gemini'nin OpenAI endpoint'i) ---
    private async Task<string> CallLlm(string apiKey, string model,
        string system, string user, int maxTokens, double temp)
    {
        var body = new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = system },
                new { role = "user", content = user }
            },
            temperature = temp,
            max_tokens = maxTokens
        };

        var http = _httpFactory.CreateClient();
        http.Timeout = TimeSpan.FromSeconds(30);

        using var req = new HttpRequestMessage(HttpMethod.Post,
            "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions");
        req.Headers.Add("Authorization", $"Bearer {apiKey}");
        req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var resp = await http.SendAsync(req);
        var json = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new Exception($"LLM API ({(int)resp.StatusCode}): {json}");

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("choices")[0]
            .GetProperty("message").GetProperty("content").GetString()?.Trim() ?? "";
    }

    // LLM bazen ```json ... ``` ile sarar, temizle
    private static string CleanJson(string s)
    {
        s = s.Trim();
        if (s.StartsWith("```"))
        {
            int first = s.IndexOf('{');
            int last = s.LastIndexOf('}');
            if (first >= 0 && last > first) s = s.Substring(first, last - first + 1);
        }
        return s;
    }

    // LLM'in dönebileceği sınırlı niyet şeması
    private class QueryIntent
    {
        public string? Metric { get; set; }
        public string? Dimension { get; set; }
        public string? FilterCity { get; set; }
        public string? FilterCategory { get; set; }
        public bool CampaignOnly { get; set; }
        public int Top { get; set; }
        public string? Order { get; set; }
    }
}