using System.Net.Http;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TeamManagement.Application.Interfaces;
using TeamManagement.Application.DTOs.AI;
using TeamManagement.Application.Common.Results;

namespace TeamManagement.Infrastructure.Services
{
    public class AiEmailParserService : IAiEmailParserService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AiEmailParserService> _logger;
        //Model appsettings üzerinden değiştirilebilir olsun diye kod içine sabitlenmedi;
        //free-tier günlük kota modelden modele çok farklı olabiliyor (bkz. Gemini:Model)
        private readonly string _model;

        //429 (rate limit) hatasında denenecek bekleme süreleri; birden fazla mail art arda
        //işlenirken dakikalık limite takılma ihtimaline karşı otomatik olarak tekrar dener
        private static readonly int[] RetryBeklemeSaniye = { 5, 15 };

        public AiEmailParserService(HttpClient httpClient, IConfiguration configuration, ILogger<AiEmailParserService> logger)
        {
            _httpClient = httpClient;
            _model = configuration["Gemini:Model"] ?? "gemini-3.5-flash";
            _logger = logger;
        }

        //429 alındığında kısa bir bekleme sonrası en fazla RetryBeklemeSaniye.Length kez tekrar dener;
        //StringContent tekrar gönderilebilir (tek kullanımlık stream değil, bayt dizisi tutar)
        private async Task<HttpResponseMessage> PostWithRetryAsync(string url, HttpContent content)
        {
            for (var deneme = 0; ; deneme++)
            {
                var response = await _httpClient.PostAsync(url, content);
                if (response.StatusCode != System.Net.HttpStatusCode.TooManyRequests || deneme >= RetryBeklemeSaniye.Length)
                    return response;

                await Task.Delay(TimeSpan.FromSeconds(RetryBeklemeSaniye[deneme]));
            }
        }

        public async Task<Result<AiTaskUpdateResultDto>> ParseEmailContentAsync(string emailContent, string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_GEMINI_API_KEY")
            {
                return Result<AiTaskUpdateResultDto>.Failure("Gemini API Key bulunamadı! Lütfen appsettings.json dosyasını kontrol edin.");
            }

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={apiKey}";

            var prompt = $@"
            Aşağıdaki e-postayı oku ve içindeki görev güncelleme bilgisini çıkar. 
            Dönüş formatı SADECE aşağıdaki gibi bir JSON olmalıdır. Başka hiçbir açıklama yazma.
            Eğer metinden çıkarılamıyorsa alanları null bırak.
            Durum sadece şunlardan biri olabilir: 'Backlog', 'DevamEdiyor', 'Test', 'OnayBekliyor', 'Tamamlandi'.

            E-posta Metni:
            {emailContent}

            Beklenen JSON Formatı:
            {{
            ""GorevId"": ""(varsa Guid, yoksa null)"",
            ""GorevBasligi"": ""(Görev adı veya başlığı)"",
            ""YeniDurum"": ""(Backlog, DevamEdiyor, Test, OnayBekliyor, Tamamlandi)"",
            ""Not"": ""(E-postadaki ilerleme notu veya yorum)""
            }}
            ";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    response_mime_type = "application/json"
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await PostWithRetryAsync(url, jsonContent);
            var responseString = await response.Content.ReadAsStringAsync();

            //Gemini'nin döndürdüğü asıl hata (kota, rate limit, geçersiz anahtar vb.) fırlatılmadan
            //önce Result olarak taşınır ki çağıran taraf (mail dinleyicileri) log'a gerçek sebebi yazabilsin
            if (!response.IsSuccessStatusCode)
                return Result<AiTaskUpdateResultDto>.Failure($"Gemini API hatası ({(int)response.StatusCode}): {responseString}");

            using var document = JsonDocument.Parse(responseString);

            var textResult = document.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text").GetString();

            if (!string.IsNullOrEmpty(textResult))
            {
                textResult = textResult.Replace("```json", "").Replace("```", "").Trim();

                var result = JsonSerializer.Deserialize<AiTaskUpdateResultDto>(textResult, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result == null)
                    return Result<AiTaskUpdateResultDto>.Failure("JSON verisi ayrıştırılamadı.");

                return Result<AiTaskUpdateResultDto>.Success(result);
            }

            return Result<AiTaskUpdateResultDto>.Failure("Modelden geçerli bir yanıt alınamadı.");
        }

        public async Task<Result<HaftalikIslerResultDto>> ParseHaftalikIslerAsync(string emailContent, string apiKey, string? mevcutGorevlerBaglami = null)
        {
            if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_GEMINI_API_KEY")
            {
                return Result<HaftalikIslerResultDto>.Failure("Gemini API Key bulunamadı! Lütfen appsettings.json dosyasını kontrol edin.");
            }

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={apiKey}";

            //Var olan görev başlıkları verilmezse boş bırakılır; AI o zaman her maddeyi bağlamsız değerlendirir
            var mevcutGorevlerBolumu = string.IsNullOrWhiteSpace(mevcutGorevlerBaglami)
                ? ""
                : $@"
            Sistemde bu ekibin şu anda AKTİF (tamamlanmamış/iptal edilmemiş) görevleri şunlar (proje: ""başlık1"", ""başlık2""):
            {mevcutGorevlerBaglami}

            Bir madde bu listedeki görevlerden birine (güncelleme/iptal/düzenleme olarak) atıfta bulunuyorsa,
            GorevBasligi/GorevBasligi alanına o görevin YUKARIDAKİ TAM başlığını harfiyen yaz — kendi kelimelerinle
            yeniden ifade etme (ör. mailde ""proje servisleri düzenlemesi"" yazsa bile listede ""Proje servislerini
            düzenleme"" olarak geçiyorsa, GorevBasligi'ne ""Proje servislerini düzenleme"" yaz). Bu, basit metin
            eşleştirmesiyle görevi bulabilmek için ZORUNLUDUR. Listede yakın bir eşleşme yoksa, bu YENİ bir görevdir.
            ÖNEMLİ: bu liste SADECE başlık eşleştirmesi içindir — mailde AÇIKÇA bahsedilmeyen bir görev için
            asla bir güncelleme/düzenleme üretme, listede olması tek başına bir sebep değildir.
            ";

            var prompt = $@"
            Aşağıdaki e-postayı oku. Bu bir haftalık iş/görev planlama e-postasıdır ve BİRDEN FAZLA proje içerebilir.
            Bugünün tarihi: {DateTime.UtcNow:yyyy-MM-dd}.
            {mevcutGorevlerBolumu}

            E-posta katı bir şablona uymayabilir:
            - Her proje genellikle kısa bir isim/kod olarak ayrı bir satırda yazılır (ör. ""GASLINE"", ""PWIRE""),
              altında da o projeyle ilgili yapılacak işler serbest metin veya madde madde sıralanır.
            - ""PROJE:"", ""EKLE:"", ""ÇIKAR:"", ""İLERLEME:"" gibi başlıklar OLMAYABİLİR. Böyle başlıklar yoksa,
              bir proje adının altındaki her satırı/maddeyi o projeye ait bir madde olarak grupla.
            - Bir madde açıkça bir görevin İPTAL edildiğini (""iptal"", ""vazgeçildi"", ""yapılmayacak"" gibi ifadelerle),
              var olan bir görevin DURUMUNU güncellediğini (""tamamlandı"", ""bitti"", ""test aşamasında"",
              ""devam ediyor"" gibi ifadelerle) ya da var olan bir görevin sadece ÖNCELİĞİNİ/TARİHİNİ değiştirmeyi
              istediğini (durumu değil — ""önceliğini yükselt"", ""tarihini X'e çek/ertele"", ""bitiş tarihini
              güncelle"" gibi ifadelerle) belirtmiyorsa, bunu iptal/güncelleme/düzenleme SAYMA — bunun yerine
              YENİ EKLENECEK bir görev olarak değerlendir. Yani varsayılan davranış her zaman ""yeni görev ekle""dir.
            - Var olan bir görevin önceliği ve/veya tarihi değiştirilmek isteniyorsa bunu DuzenlenecekGorevler
              listesine ekle; belirtilmeyen alanları null bırak (mevcut değer korunur). Bu, GuncellenecekGorevler'i
              DIŞLAMAZ — aynı cümle/madde HEM bir durum değişikliğinden (""test aşamasındayım"" gibi) HEM bir
              tarih/öncelikten (""haftaya cuma tamamlarım"" gibi) bahsediyorsa, o görev için AYNI ANDA hem
              GuncellenecekGorevler'e (durum) hem DuzenlenecekGorevler'e (tarih) birer kayıt ekle — ikisi de
              gerçekleşmeli, sadece biri değil.
            - Görevin başlangıç ve bitiş tarihleri belirtilmişse 'YYYY-MM-DD' formatında al, yoksa null bırak.
              Tarihte yıl belirtilmemişse (ör. sadece ""26 Ağustos""), yukarıdaki bugünün tarihinin YILINI kullan;
              eğer o ay/gün bu yıl için bugünün tarihinden önce geçmişse bir SONRAKİ yılı kullan (geçmiş tarih üretme).
            - Öncelik belirtilmişse al, yoksa 'Normal' yap.
            - YeniDurum sadece şunlardan biri olabilir: 'Backlog', 'DevamEdiyor', 'Test', 'OnayBekliyor', 'Tamamlandi', 'Iptal'.
            - Yeni eklenecek bir görev için mailde ""(isim) yapacak"", ""(isim)'e ver"", ""(isim) ilgilenecek"" gibi
              bir kişiye atandığı açıkça belirtilmişse, o kişinin adını 'Atanan' alanına yaz. Mailde soyadı da
              geçiyorsa MUTLAKA soyadıyla birlikte yaz (ör. ""Ahmet Yılmaz"", sadece ""Ahmet"" değil) — aynı adı
              taşıyan birden fazla ekip üyesi olabileceğinden sadece ad yeterli olmayabilir. Belirtilmemişse null bırak.

            Dönüş formatı SADECE aşağıdaki gibi bir JSON olmalıdır. Başka hiçbir açıklama yazma.

            E-posta Metni:
            {emailContent}

            Beklenen JSON Formatı:
            {{
                ""Projeler"": [
                    {{
                        ""ProjeAdi"": ""(Projenin adı/kodu)"",
                        ""EklenecekGorevler"": [
                            {{
                                ""Baslik"": ""(Görev başlığı)"",
                                ""BaslangicTarihi"": ""(YYYY-MM-DD veya null)"",
                                ""BitisTarihi"": ""(YYYY-MM-DD veya null)"",
                                ""Oncelik"": ""(Yuksek, Normal, Dusuk)"",
                                ""Atanan"": ""(varsa görevin atandığı kişinin adı, yoksa null)""
                            }}
                        ],
                        ""IptalEdilenGorevler"": [
                            ""(Görev başlığı)""
                        ],
                        ""GuncellenecekGorevler"": [
                            {{
                                ""GorevBasligi"": ""(Görev başlığı)"",
                                ""YeniDurum"": ""(Backlog, DevamEdiyor, vs.)"",
                                ""Not"": ""(Varsa açıklama)""
                            }}
                        ],
                        ""DuzenlenecekGorevler"": [
                            {{
                                ""GorevBasligi"": ""(Görev başlığı)"",
                                ""YeniOncelik"": ""(Yuksek, Normal, Dusuk veya null)"",
                                ""YeniBaslangicTarihi"": ""(YYYY-MM-DD veya null)"",
                                ""YeniBitisTarihi"": ""(YYYY-MM-DD veya null)""
                            }}
                        ]
                    }}
                ]
            }}
            ";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    response_mime_type = "application/json"
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await PostWithRetryAsync(url, jsonContent);
            var responseString = await response.Content.ReadAsStringAsync();

            //Gemini'nin döndürdüğü asıl hata (kota, rate limit, geçersiz anahtar vb.) fırlatılmadan
            //önce Result olarak taşınır ki çağıran taraf (mail dinleyicileri) log'a gerçek sebebi yazabilsin
            if (!response.IsSuccessStatusCode)
                return Result<HaftalikIslerResultDto>.Failure($"Gemini API hatası ({(int)response.StatusCode}): {responseString}");

            using var document = JsonDocument.Parse(responseString);

            var textResult = document.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text").GetString();

            if (!string.IsNullOrEmpty(textResult))
            {
                textResult = textResult.Replace("```json", "").Replace("```", "").Trim();
                //AI'ın ayrıştırdığı JSON burada loglanır ki bir görev "kayboldu" gibi görünen
                //durumlarda (ör. AI yanlış projeye/başlığa yazdı) sebep kodda değil, gerçekten
                //AI'ın çıktısında mı diye ayrım yapılabilsin
                _logger.LogInformation($"Gemini haftalık plan JSON çıktısı: {textResult}");

                var result = JsonSerializer.Deserialize<HaftalikIslerResultDto>(textResult, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result == null)
                    return Result<HaftalikIslerResultDto>.Failure("JSON verisi ayrıştırılamadı.");

                return Result<HaftalikIslerResultDto>.Success(result);
            }

            return Result<HaftalikIslerResultDto>.Failure("Modelden geçerli bir yanıt alınamadı.");
        }
    }
}
