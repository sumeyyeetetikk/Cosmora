# 💄 Cosmora

Kozmetik & drogeri perakendesi için bir satış zekâsı platformu. ASP.NET Core MVC üzerine kurulmuş, EF Core + MSSQL ile 1.000.000 desenli satış kaydını yöneten; üzerine **beş ayrı ML.NET modeli** (SSA tahmini, ikili/çoklu sınıflandırma, anomali tespiti, kümeleme) ve **iki yapay zeka asistanı** (doğal dilde satış analizi ve güvenli text-to-SQL sorgulama) eklenmiş tam kapsamlı bir .NET 8 uygulaması.

## ✨ Öne Çıkanlar

🏗️ **Star Schema + Tek Katman Disiplini** — Category, Product, City boyutları + Sale fact tablosu; controller'lar ince, tüm ML ve veri mantığı arayüz arkasındaki servislerde.

🎲 **1M Desenli Veri** — Mevsimsellik, şehir farkı, kampanya/indirim etkisi, hafta sonu etkisi ve %2 aykırı değer içeren gerçekçi satış verisi; `SqlBulkCopy` ile ~30 saniyede üretilir, hiçbir zaman belleğe alınmaz.

📈 **SSA ile 7 Günlük Tahmin** — Seçilen şehrin günlük satış serisinden güven sınırlarıyla birlikte gelecek tahmini.

✅ **İkili Sınıflandırma** — Bir şehrin gelecek ay 7.000 adet eşiğini aşıp aşmayacağı; `SdcaLogisticRegression`, Accuracy/Precision/Recall/F1/AUC + Confusion Matrix.

🎯 **Çoklu Sınıflandırma** — Gelecek ay performansı Düşük/Orta/Yüksek; sınıf sınırları veri dağılımının persentillerinden belirlenir.

⚠️ **Anomali Tespiti** — SR-CNN ile olağandışı satış günlerinin (sıçrama/çöküş) yakalanması.

🗺️ **K-Means Kümeleme** — 30 şehrin satış hacmine göre segmentasyonu (metropol → küçük şehir).

🤖 **2 Yapay Zeka Asistanı** — Doğal dilde satış analizi + güvenli text-to-SQL soru-cevap.

## 🏗️ Mimari

Proje **tek katmanlı** ama disiplinli bir yapıdadır. Controller'lar iş mantığı içermez; tüm veri ve ML mantığı arayüz arkasındaki servislerde toplanır (`IForecastService`, `IBinaryClassificationService`, `IMulticlassService`, `IAnomalyService`, `IClusterService`, `IAiAnalysisService`, `ISalesChatService`).

```
Controller (ince)  →  Service (iş + ML mantığı)  →  EF Core (SQL aggregate)  →  MSSQL
                                   ↓
                            ML.NET pipeline
```

Veri modeli **star schema** olarak kurulmuştur: `Category`, `Product`, `City` boyut tabloları + `Sale` fact tablosu. Tüm aggregation'lar SQL tarafında yapılır — 1M satır hiçbir zaman belleğe gelmez. ML servislerinde "bugün" referansı `DateTime.Now`'dan değil verideki `MAX(OrderDate)`'ten alınır; böylece tahmin ve pencereler daima veriyle hizalıdır.

## 🛠️ Teknoloji Yığını

| Kategori | Teknoloji |
|---|---|
| Framework | .NET 8, ASP.NET Core MVC |
| ORM | Entity Framework Core |
| Veritabanı | SQL Server (LocalDB / Express) |
| Makine Öğrenmesi | ML.NET (`Microsoft.ML`, `Microsoft.ML.TimeSeries`) |
| Toplu Yükleme | `SqlBulkCopy` (`System.Data.SqlClient`) |
| Grafik | Chart.js |
| LLM (analiz) | Groq — Llama 3.3 70B (OpenAI uyumlu) |
| LLM (sorgu) | Google Gemini (OpenAI uyumlu) |

**Kısıtlar:** Çekirdek ML görevleri Python veya ML.NET Model Builder kullanmadan, tamamen kod tarafında manuel kurulan pipeline'larla geliştirilmiştir.

## 🎲 Veri Üretimi

Veri desenli üretilir (tamamen random değil). Talep, çarpanların çarpımıyla oluşur:

```
Quantity ≈ 3.0 × Popularity × MevsimÇarpanı × HaftaSonu × Kampanya × İndirim × Gürültü
```

Ürün popülerliği, şehir ağırlığı, mevsimsel zirveler (yaz güneş ürünleri, kış bakımı, hediye dönemleri), kampanya-indirim ilişkisi ve %2 aykırı değer C# tarafında hesaplanır. 1M satır listede biriktirilmez; 50.000'lik batch'ler `DataTable`'a doldurulup `SqlBulkCopy` ile yazılır. Seeder "yoksa üret, varsa dokunma" prensibiyle çalışır ve yarım kalan seed'i otomatik onarır. Tarih aralığı 15.08.2023 – 15.08.2026 (3 tam yıl).

## 🤖 Makine Öğrenmesi Görevleri

Uygulama, kod tarafında manuel kurulan beş ayrı ML.NET modeli içerir.

**1. 📈 Satış Tahmini (SSA)** — Seçilen şehrin günlük toplam satış serisi (SQL'de gruplanır, ~1095 gün) Singular Spectrum Analysis ile 7 gün ileri tahmin edilir. Parametreler (windowSize=7, seriesLength=30, horizon=7, confidenceLevel=0.95) ekranda açıklanır. Çıktı: son 30 gün gerçek + 7 gün tahmin, güven bandıyla aynı grafikte.

**2. ✅ İkili Sınıflandırma** — "Seçilen şehir gelecek ay 7.000 adet eşiğini aşacak mı?" Feature'lar SQL'de kayan pencereyle üretilir (son 3 ayın satışları, 3 aylık ortalama, hedef ay). `SdcaLogisticRegression` ile eğitilir; %80/%20 train-test split sonrası Accuracy, Precision, Recall, F1, AUC ve Confusion Matrix raporlanır. Birim, veri dağılımı analiz edilerek "şehir-ay" olarak seçilmiştir; 7.000 eşiği bu kırılımda dengeli dağılır.

**3. 🎯 Çoklu Sınıflandırma** — Gelecek ay performansı Düşük / Orta / Yüksek. Aynı feature altyapısı; sınıf sınırları veri dağılımının %33 ve %66 persentillerinden hesaplanıp ekranda gösterilir (dengeli, savunulabilir problem). `SdcaMaximumEntropy` ile MicroAccuracy, MacroAccuracy, LogLoss + 3×3 Confusion Matrix.

**4. ⚠️ Anomali Tespiti** — Günlük satış serisindeki olağandışı sıçrama/çöküşleri `DetectEntireAnomalyBySrCnn` (SR-CNN) ile yakalar. Threshold veri üzerinde kalibre edilmiştir: enjekte edilen aykırı değerleri yakalar ama normal mevsimsel dalgayı anomali saymaz. Çıktı: tarih, gerçek/beklenen satış, yön ve anomali skoru.

**5. 🗺️ Kümeleme (K-Means)** — 30 şehir, hacim temelli feature'larla (ortalama günlük satış, 3 yıllık toplam, en yoğun gün) 4 segmente ayrılır: Metropoller, Büyük, Orta, Küçük şehirler. Özellikler farklı ölçekte olduğu için Min-Max normalizasyon uygulanır ve kümeler yorumlanabilir etiketlerle sunulur.

## 🧠 Yapay Zeka Asistanları

LLM istemcisi OpenAI uyumlu formatta yazıldığından sağlayıcı (Groq / Gemini) kolayca değiştirilebilir. Her iki asistan da yalnızca gerçek verilere dayanır, uydurma yapmaz.

**1. 🎭 AI Satış Analizi (Groq · Llama 3.3)** — Tek butonla backend son 30 günün satış verisini, en çok satan ürünleri, şehir/kategori kırılımlarını ve yaklaşan sezon bilgisini toplar; LLM'e gönderir ve yöneticiye doğal dilde genel durum, dikkat çekenler ve aksiyon önerileri üretir.

**2. 💬 Veriye Doğal Dil Sorgu (Google Gemini)** — Kullanıcı gündelik dille soru sorar ("İstanbul'da en çok satan 5 ürün"). Güvenli **structured-intent** mimarisiyle çalışır: LLM ham SQL üretmez, yalnızca sınırlı bir JSON niyet döner; gerçek sorguyu uygulama kodu kurar (yalnızca Sale üzerinde, yalnızca aggregate). Bu, prompt injection'ı engeller. Ekrandaki şeffaflık kutuları LLM'in çıkardığı niyeti ve çekilen gerçek veriyi canlı gösterir.


---


<img src="https://github.com/user-attachments/assets/e1abf514-f4b7-45e2-a673-46bd67c52742" width="800" style="border:1px solid #ddd; border-radius:8px" />


---

<img width="1054" height="1522" alt="Ekran görüntüsü 2026-08-16 020359" src="https://github.com/user-attachments/assets/b19976c4-8953-4106-a275-1ab3978f621f" />

---

<img width="1055" height="1068" alt="Ekran görüntüsü 2026-08-16 020240" src="https://github.com/user-attachments/assets/029ce59f-8de5-46d1-893c-d36b08f7ce28" />

---

<img width="1066" height="1361" alt="Ekran görüntüsü 2026-08-16 020210" src="https://github.com/user-attachments/assets/a1eb2574-d07f-4877-8198-246f9aca12bb" />

---

<img width="1054" height="1431" alt="Ekran görüntüsü 2026-08-16 020137" src="https://github.com/user-attachments/assets/9c5119f2-e54c-4a0f-8228-e535a70d200e" />

---

<img width="1056" height="1327" alt="Ekran görüntüsü 2026-08-16 020107" src="https://github.com/user-attachments/assets/8abd76a0-1012-42ba-99d3-85eb16376775" />

---

<img width="1898" height="909" alt="Ekran görüntüsü 2026-08-16 020008" src="https://github.com/user-attachments/assets/50427e24-5d67-4c07-afb9-0d3f14cccd13" />

---

<img width="1889" height="909" alt="Ekran görüntüsü 2026-08-16 015928" src="https://github.com/user-attachments/assets/20f82c0f-c668-49c1-a87a-78853bf7faf7" />

---

<img width="1904" height="907" alt="Ekran görüntüsü 2026-08-16 015857" src="https://github.com/user-attachments/assets/4de0b128-17d9-482e-8a7f-8bc1f4e02c1c" />

---

<img width="1898" height="902" alt="Ekran görüntüsü 2026-08-16 015832" src="https://github.com/user-attachments/assets/3f89d380-2077-49ae-bccf-afba92263e9d" />


###


###


###





































