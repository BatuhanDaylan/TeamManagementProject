using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TeamManagement.Application.DTOs.Gorevler;
using TeamManagement.Application.Interfaces;
using TeamManagement.Domain.Entities;
using TeamManagement.Domain.Enums;
using TeamManagement.Infrastructure.Helpers;
using TeamManagement.Infrastructure.Persistence;

namespace TeamManagement.Infrastructure.BackgroundServices
{
    //Mevcut EmailListenerBackgroundService ortak bot kutusunu (Inbox) dinlemeye devam eder, dokunulmadı.
    //Bu servis ise mail/AI ayarlarını yapılandırmış her lider için AYRI bir IMAP bağlantısı açıp
    //liderin KENDİ Gönderilenler (Sent) klasöründeki haftalık plan mailini dinler.
    public class HaftalikPlanMailListenerBackgroundService : BackgroundService
    {
        private readonly ILogger<HaftalikPlanMailListenerBackgroundService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;

        //Aynı taramada art arda birden fazla mail Gemini'ye gönderilecekse aralarına küçük bir
        //bekleme konur; birkaç kişi aynı anda mail atarsa dakikalık istek limitine baştan çarpmayı önler
        private static readonly TimeSpan AiIstekleriArasiBekleme = TimeSpan.FromSeconds(3);

        //Bazı Outlook/kurumsal mail istemcileri konu başlığını Windows-1254 (Türkçe) ile kodlarken
        //sunucu/parser bunu ISO-8859-1 sanıp yanlış çözüyor; sonuç olarak ı/İ/ş/Ş/ğ/Ğ harfleri
        //sırasıyla ý/Ý/þ/Þ/ð/Ð olarak bozuk gelir (ör. "Haftalık" -> "Haftalýk"). Anahtar kelime
        //eşleşmesi bu yüzden hiç tutmaz ve gerçek ekip üyelerinden gelen mailler sessizce atlanır.
        //Bu dönüşüm sadece anahtar kelime eşleşmesi için kullanılır, mailin gerçek içeriği değişmez.
        //İkinci bir mojibake kalıbı: bazı mail istemcileri konuyu farklı bir Türkçe kod sayfasıyla
        //kodluyor, bu da ı/İ/ş harflerinin ¹/©/º olarak bozulmasına yol açıyor (ör. gerçek bir
        //üye mailinde görülen "Haftal¹k Yap¹lacak ©ºler" -> "Haftalık Yapılacak İşler").
        private static string TurkceMojibakeDuzelt(string metin) => metin
            .Replace('ý', 'ı').Replace('Ý', 'İ')
            .Replace('þ', 'ş').Replace('Þ', 'Ş')
            .Replace('ð', 'ğ').Replace('Ð', 'Ğ')
            .Replace('¹', 'ı').Replace('©', 'İ').Replace('º', 'ş');

        public HaftalikPlanMailListenerBackgroundService(
            ILogger<HaftalikPlanMailListenerBackgroundService> logger,
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("HaftalikPlanMailListenerBackgroundService başlatıldı. (Lider bazlı Gönderilenler dinleyici)");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await TumLiderleriTaraAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Lider bazlı mail taraması sırasında genel hata: {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
        }

        private async Task TumLiderleriTaraAsync(CancellationToken cancellationToken)
        {
            var host = _configuration["EmailSettings:ImapServer"];
            var portString = _configuration["EmailSettings:ImapPort"];
            var useSslString = _configuration["EmailSettings:UseSsl"];

            if (string.IsNullOrEmpty(host))
            {
                _logger.LogWarning("IMAP sunucu ayarları yapılandırılmamış, lider bazlı tarama atlanıyor.");
                return;
            }

            var port = string.IsNullOrEmpty(portString) ? 993 : int.Parse(portString);
            var useSsl = string.IsNullOrEmpty(useSslString) || bool.Parse(useSslString);

            //AI kotasını boşa harcamamak için, konusu bu anahtar kelimelerden hiçbirini içermeyen gönderilmiş
            //mailler AI'a hiç gönderilmez. Birden fazla anahtar kelime tanımlanabilir ki "Haftalık İşler",
            //"Haftalık Görevler" gibi varyasyonlar da yakalansın.
            var turkce = new System.Globalization.CultureInfo("tr-TR");
            var haftalikPlanAnahtarlari = (_configuration.GetSection("EmailSettings:HaftalikPlanKonuAnahtarlari").Get<string[]>()
                ?? new[] { "haftalık" }).Select(k => k.ToLower(turkce)).ToArray();
            //Üyelerin lidere attığı tekil durum güncelleme mailleri bu anahtar kelimelerle tanınır
            var gorevGuncellemeAnahtarlari = (_configuration.GetSection("EmailSettings:GorevGuncellemeKonuAnahtarlari").Get<string[]>()
                ?? new[] { "görev güncelle", "durum güncelle" }).Select(k => k.ToLower(turkce)).ToArray();

            using var scope = _scopeFactory.CreateScope();
            var identityContext = scope.ServiceProvider.GetRequiredService<TeamManagementIdentityDbContext>();
            var korumaService = scope.ServiceProvider.GetRequiredService<IGizliVeriKorumaService>();

            //Her ikisi de yapılandırılmamış liderler bu akışa dahil edilmez
            var liderler = await identityContext.Kullanicilar
                .Where(k => k.Rol == Rol.Lider
                    && k.MailUygulamaSifresiSifreli != null
                    && k.AiApiKeySifreli != null
                    && k.EkipId != null)
                .ToListAsync(cancellationToken);

            foreach (var lider in liderler)
            {
                try
                {
                    await TekLideriTaraAsync(lider, host, port, useSsl, haftalikPlanAnahtarlari, gorevGuncellemeAnahtarlari, korumaService, identityContext, scope.ServiceProvider, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"'{lider.Email}' için haftalık plan taraması başarısız: {ex.Message}");
                }
            }
        }

        private async Task TekLideriTaraAsync(
            Kullanici lider,
            string host,
            int port,
            bool useSsl,
            string[] haftalikPlanAnahtarlari,
            string[] gorevGuncellemeAnahtarlari,
            IGizliVeriKorumaService korumaService,
            TeamManagementIdentityDbContext identityContext,
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken)
        {
            var uygulamaSifresi = korumaService.SifreCoz(lider.MailUygulamaSifresiSifreli);
            var aiApiKey = korumaService.SifreCoz(lider.AiApiKeySifreli);
            if (string.IsNullOrEmpty(uygulamaSifresi) || string.IsNullOrEmpty(aiApiKey))
                return;

            using var client = new ImapClient();
            await client.ConnectAsync(host, port, useSsl, cancellationToken);
            await client.AuthenticateAsync(lider.Email, uygulamaSifresi, cancellationToken);

            var aiParser = serviceProvider.GetRequiredService<IAiEmailParserService>();
            var gorevService = serviceProvider.GetRequiredService<IGorevService>();
            var projeService = serviceProvider.GetRequiredService<IProjeService>();

            //Mailde "Atanan" olarak geçen isim, ekip üyeleriyle eşleştirilebilsin diye taban listesi
            //bir kez çekilir (tarama boyunca ekip üyeliği değişmeyeceği için iki klasör taraması için de ortak)
            var ekipUyeleri = await identityContext.Kullanicilar
                .Where(k => k.EkipId == lider.EkipId)
                .ToListAsync(cancellationToken);
            var ekipUyeListesi = ekipUyeleri.Select(k => (k.Id, k.Ad, k.Soyad)).ToList();

            await SentKlasoruTaraAsync(client, lider, aiApiKey, haftalikPlanAnahtarlari, ekipUyeListesi,
                aiParser, gorevService, projeService, identityContext, cancellationToken);

            await InboxTaraAsync(client, lider, aiApiKey, haftalikPlanAnahtarlari, gorevGuncellemeAnahtarlari, ekipUyeleri, ekipUyeListesi,
                aiParser, gorevService, projeService, identityContext, cancellationToken);

            await client.DisconnectAsync(true, cancellationToken);
        }

        //Liderin kendi haftalık plan mailini attığı Gönderilenler klasörünü tarar (mevcut davranış, değişmedi)
        private async Task SentKlasoruTaraAsync(
            ImapClient client,
            Kullanici lider,
            string aiApiKey,
            string[] haftalikPlanAnahtarlari,
            List<(Guid Id, string Ad, string Soyad)> ekipUyeListesi,
            IAiEmailParserService aiParser,
            IGorevService gorevService,
            IProjeService projeService,
            TeamManagementIdentityDbContext identityContext,
            CancellationToken cancellationToken)
        {
            IMailFolder sentFolder;
            try
            {
                sentFolder = client.GetFolder(SpecialFolder.Sent);
            }
            catch (Exception)
            {
                _logger.LogWarning($"'{lider.Email}' için Gönderilenler klasörü bulunamadı, bu klasör atlanıyor.");
                return;
            }

            await sentFolder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

            //Klasör ilk kez taranıyorsa ya da UIDVALIDITY değişmişse (klasör sıfırlanmış olabilir):
            //geçmiş mailler toptan işlenmesin diye sadece mevcut en yüksek UID taban alınır, hiçbir mail işlenmez
            var mevcutUidValidity = (long)sentFolder.UidValidity;
            if (lider.SonIslenenMailUidValidity != mevcutUidValidity)
            {
                lider.SonIslenenMailUidValidity = mevcutUidValidity;
                lider.SonIslenenMailUid = sentFolder.UidNext.HasValue ? (long)sentFolder.UidNext.Value.Id - 1 : 0;
                await identityContext.SaveChangesAsync(cancellationToken);
                return;
            }

            var baslangicUid = new UniqueId((uint)(lider.SonIslenenMailUid!.Value + 1));

            //ÖNEMLİ: "baslangicUid:MaxValue" aralığını, başlangıç zaten en yeni mail işlenmişken de
            //sorgulamak Gmail'in IMAP sunucusunda RFC 3501 gereği aralığın küçükten büyüğe normalize
            //edilmesine (ör. "58:57" -> "57:58") ve son mailin sonsuza kadar tekrar eşleşmesine yol açar.
            //Bu yüzden gerçekten yeni bir mail olup olmadığı UidNext ile önceden kontrol edilir.
            if (sentFolder.UidNext.HasValue && baslangicUid.Id >= sentFolder.UidNext.Value.Id)
                return;

            var uidler = await sentFolder.SearchAsync(SearchQuery.Uids(new UniqueIdRange(baslangicUid, UniqueId.MaxValue)), cancellationToken);
            if (uidler.Count == 0)
                return;

            foreach (var uid in uidler.OrderBy(u => u.Id))
            {
                try
                {
                    var message = await sentFolder.GetMessageAsync(uid, cancellationToken);
                    var textBody = message.TextBody ?? message.HtmlBody ?? "Boş mail";
                    var subjectLower = TurkceMojibakeDuzelt(message.Subject ?? "").ToLower(new System.Globalization.CultureInfo("tr-TR"));

                    //Sistemin kendi gönderdiği otomatik haftalık özet maili de liderin Gönderilenler
                    //klasörüne düşer; konusu "haftalık" içerdiği için AI'a yanlışlıkla plan mail'i gibi
                    //gönderilmemesi için burada ayrıca elenir
                    var otomatikOzetMaili = subjectLower.Contains("ekip özeti");

                    if (!otomatikOzetMaili && haftalikPlanAnahtarlari.Any(k => subjectLower.Contains(k)))
                    {
                        _logger.LogInformation($"'{lider.Email}' Gönderilenler'de haftalık plan maili bulundu: '{message.Subject}'");
                        await HaftalikPlaniIsleAsync(lider, textBody, aiApiKey, aiParser, gorevService, projeService, ekipUyeListesi);
                        //Aynı turda işlenecek başka mail varsa Gemini'ye art arda gitmesin diye kısa bir bekleme
                        await Task.Delay(AiIstekleriArasiBekleme, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    //Tek bir mailin işlenmesi başarısız olsa bile (örn. AI kota hatası) watermark yine de
                    //ilerletilmeli; aksi halde aynı mail sonsuza kadar tekrar denenir ve sonraki mailleri
                    //de bloklar (kota tükenene kadar sürekli aynı hatayı üretir)
                    _logger.LogWarning($"'{lider.Email}': Gönderilenler UID {uid} işlenemedi, atlanıyor: {ex.Message}");
                }

                //Watermark her mail sonrası ayrı ayrı kaydedilir; döngü sonuna bırakılırsa ve o tek
                //kayıt herhangi bir sebeple (bağlantı kopması, servis yeniden başlaması vb.) başarısız
                //olursa, bu turda zaten oluşturulmuş görevler bir sonraki taramada MÜKERRER oluşturulur
                lider.SonIslenenMailUid = (long)uid.Id;
                await identityContext.SaveChangesAsync(cancellationToken);
            }
        }

        //Liderin kendi Gelen Kutusu'nu tarar; üyelerin lidere attığı hem tekil "durum güncelle"
        //mailleri hem de kendi haftalık plan mailleri buradan yakalanır. Sadece liderin KENDİ
        //ekibindeki üyelerden gelen mailler işlenir.
        private async Task InboxTaraAsync(
            ImapClient client,
            Kullanici lider,
            string aiApiKey,
            string[] haftalikPlanAnahtarlari,
            string[] gorevGuncellemeAnahtarlari,
            List<Kullanici> ekipUyeleri,
            List<(Guid Id, string Ad, string Soyad)> ekipUyeListesi,
            IAiEmailParserService aiParser,
            IGorevService gorevService,
            IProjeService projeService,
            TeamManagementIdentityDbContext identityContext,
            CancellationToken cancellationToken)
        {
            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

            var mevcutUidValidity = (long)inbox.UidValidity;
            if (lider.SonIslenenInboxUidValidity != mevcutUidValidity)
            {
                lider.SonIslenenInboxUidValidity = mevcutUidValidity;
                lider.SonIslenenInboxUid = inbox.UidNext.HasValue ? (long)inbox.UidNext.Value.Id - 1 : 0;
                await identityContext.SaveChangesAsync(cancellationToken);
                return;
            }

            var baslangicUid = new UniqueId((uint)(lider.SonIslenenInboxUid!.Value + 1));

            //Sent klasöründeki aynı RFC 3501 aralık-normalizasyonu riskine karşı aynı önlem
            if (inbox.UidNext.HasValue && baslangicUid.Id >= inbox.UidNext.Value.Id)
                return;

            var uidler = await inbox.SearchAsync(SearchQuery.Uids(new UniqueIdRange(baslangicUid, UniqueId.MaxValue)), cancellationToken);
            if (uidler.Count == 0)
                return;

            var turkce = new System.Globalization.CultureInfo("tr-TR");

            foreach (var uid in uidler.OrderBy(u => u.Id))
            {
                try
                {
                    var message = await inbox.GetMessageAsync(uid, cancellationToken);
                    var subjectLower = TurkceMojibakeDuzelt(message.Subject ?? "").ToLower(turkce);
                    //Sistemin kendi gönderdiği otomatik haftalık özet maili, leader kendine gönderdiği
                    //için Gelen Kutusu'na da düşer; "haftalık" içerdiği için AI'a boşuna gönderilmesin
                    var otomatikOzetMaili = subjectLower.Contains("ekip özeti");
                    var mailIlgiliMi = !otomatikOzetMaili && (haftalikPlanAnahtarlari.Any(k => subjectLower.Contains(k))
                        || gorevGuncellemeAnahtarlari.Any(k => subjectLower.Contains(k)));

                    if (mailIlgiliMi)
                    {
                        //Gönderen, liderin kendi ekibindeki bir üye olmalı; aksi halde sahiplik doğrulaması
                        //atlanmış (herkes lidere mail atıp görev güncelleyebilir/oluşturabilir) olur
                        var gondericiEmail = message.From.Mailboxes.FirstOrDefault()?.Address;
                        var gonderici = ekipUyeleri.FirstOrDefault(u =>
                            string.Equals(u.Email, gondericiEmail, StringComparison.OrdinalIgnoreCase));

                        if (gonderici == null)
                        {
                            _logger.LogInformation($"'{lider.Email}': Gelen Kutusu'ndaki '{message.Subject}' maili tanınmayan/farklı ekipten bir gönderene ait, atlanıyor.");
                        }
                        else
                        {
                            var textBody = message.TextBody ?? message.HtmlBody ?? "Boş mail";

                            if (haftalikPlanAnahtarlari.Any(k => subjectLower.Contains(k)))
                            {
                                //Üye kendi yapacağı işleri bildiriyor: yeni oluşturulan görevler, mailde başka
                                //bir isim açıkça belirtilmediği sürece varsayılan olarak gönderenin kendisine atanır
                                _logger.LogInformation($"'{lider.Email}': Gelen Kutusu'nda '{gonderici.Email}' tarafından haftalık plan maili bulundu: '{message.Subject}'");
                                await HaftalikPlaniIsleAsync(gonderici, textBody, aiApiKey, aiParser, gorevService, projeService, ekipUyeListesi, varsayilanAtananId: gonderici.Id);
                                //Aynı turda işlenecek başka mail varsa Gemini'ye art arda gitmesin diye kısa bir bekleme
                                await Task.Delay(AiIstekleriArasiBekleme, cancellationToken);
                            }
                            else
                            {
                                var aiResult = await aiParser.ParseEmailContentAsync(textBody, aiApiKey);
                                await Task.Delay(AiIstekleriArasiBekleme, cancellationToken);

                                if (aiResult.IsSuccess && aiResult.Data != null
                                    && !string.IsNullOrEmpty(aiResult.Data.YeniDurum) && !string.IsNullOrEmpty(aiResult.Data.GorevBasligi)
                                    && Enum.TryParse<GorevDurumu>(aiResult.Data.YeniDurum, true, out var yeniDurumEnum))
                                {
                                    var resultUpdate = await gorevService.UpdateGorevDurumByBaslikAsync(
                                        aiResult.Data.GorevBasligi, yeniDurumEnum, aiResult.Data.Not,
                                        gonderici.Id, lider.EkipId, gonderici.Rol.ToString(), textBody);

                                    if (resultUpdate.IsSuccess)
                                        _logger.LogInformation($"'{lider.Email}': '{gonderici.Email}' gönderdi, durum güncellendi: '{aiResult.Data.GorevBasligi}' -> {yeniDurumEnum}");
                                    else
                                        _logger.LogWarning($"'{lider.Email}': '{gonderici.Email}' → '{aiResult.Data.GorevBasligi}' isimli görev bulunamadı.");
                                }
                                else
                                {
                                    _logger.LogWarning($"'{lider.Email}': '{gonderici.Email}' mailinden geçerli bir görev güncellemesi çıkarılamadı: {aiResult.Error}");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"'{lider.Email}': Gelen Kutusu UID {uid} işlenemedi, atlanıyor: {ex.Message}");
                }

                //Watermark her mail sonrası ayrı ayrı kaydedilir (bkz. SentKlasoruTaraAsync'teki aynı not)
                lider.SonIslenenInboxUid = (long)uid.Id;
                await identityContext.SaveChangesAsync(cancellationToken);
            }
        }

        //Mevcut EmailListenerBackgroundService'teki haftalık plan işleme akışının aynısı — proje/görev
        //eşleştirmesi doğrudan yazanın kendi EkipId'siyle sınırlanır. "yazan" liderin kendisi (Gönderilenler
        //akışı) ya da bir üye (Gelen Kutusu akışı) olabilir; varsayilanAtananId verilirse, mailde açıkça
        //başka bir isim geçmeyen yeni görevler otomatik olarak bu kişiye (genelde mailin yazarına) atanır
        private async Task HaftalikPlaniIsleAsync(
            Kullanici yazan,
            string textBody,
            string aiApiKey,
            IAiEmailParserService aiParser,
            IGorevService gorevService,
            IProjeService projeService,
            List<(Guid Id, string Ad, string Soyad)> ekipUyeleri,
            Guid? varsayilanAtananId = null)
        {
            //AI'a mevcut aktif görev başlıkları önceden verilir; aksi halde AI aynı görevi az farklı bir
            //kelime sırasıyla yazınca (ör. "servislerini düzenleme" yerine "servisleri düzenlemesi") başlık
            //eşleştirmesi (Contains) tutmuyor ve güncelleme/iptal/düzenleme sessizce "görev bulunamadı" oluyordu
            var (mevcutGorevlerBaglami, aktifGorevler) = await MevcutGorevBasliklariniListeleAsync(yazan.EkipId, gorevService, projeService);

            var aiResult = await aiParser.ParseHaftalikIslerAsync(textBody, aiApiKey, mevcutGorevlerBaglami);
            if (!aiResult.IsSuccess || aiResult.Data == null)
            {
                _logger.LogWarning($"'{yazan.Email}': haftalık plan AI tarafından ayrıştırılamadı: {aiResult.Error}");
                return;
            }

            //Mail birden fazla proje icerebilir, her proje bolumu ayri ayri islenir; bir projenin
            //bulunamamasi diger projelerin islenmesini engellemez
            foreach (var projePlani in aiResult.Data.Projeler)
            {
                if (string.IsNullOrEmpty(projePlani.ProjeAdi))
                    continue;

                var projeResult = await projeService.GetProjeByNameAsync(projePlani.ProjeAdi, yazan.EkipId);
                if (!projeResult.IsSuccess || projeResult.Data == null)
                {
                    _logger.LogWarning($"'{yazan.Email}': '{projePlani.ProjeAdi}' adında bir proje kendi ekibinde bulunamadı.");
                    continue;
                }

                var proje = projeResult.Data;

                foreach (var eklenecek in projePlani.EklenecekGorevler)
                {
                    //Aynı görev başka bir haftada/mailde tekrar "yeni görev" gibi bildirilebilir (üye henüz
                    //bitirmediği bir işi her hafta özetinde tekrar yazabilir); aynı projede zaten aktif
                    //(Tamamlandi/Iptal olmayan) ve başlığı örtüşen bir görev varsa mükerrer eklenmez, atlanır
                    var basliginSadeHali = eklenecek.Baslik.ToLower();
                    var zatenVarMi = aktifGorevler.Any(g =>
                        g.ProjeId == proje.Id &&
                        (g.Baslik.ToLower().Contains(basliginSadeHali) || basliginSadeHali.Contains(g.Baslik.ToLower())));

                    if (zatenVarMi)
                    {
                        _logger.LogInformation($"'{yazan.Email}': '{eklenecek.Baslik}' zaten aktif bir görev olarak mevcut, mükerrer eklenmedi.");
                        continue;
                    }

                    //Tek bir görevin eklenmesi başarısız olsa bile (ör. beklenmeyen veri) aynı mailin
                    //diğer maddeleri işlenmeye devam etsin
                    try
                    {
                        var atananId = KullaniciEslestirmeHelper.EslesenKullaniciId(ekipUyeleri, eklenecek.Atanan) ?? varsayilanAtananId;
                        var yeniGorev = new CreateGorevDto
                        {
                            Baslik = eklenecek.Baslik,
                            Aciklama = "Haftalık plan mailinden eklendi.",
                            Oncelik = eklenecek.Oncelik,
                            BaslangicTarihi = eklenecek.BaslangicTarihi,
                            Deadline = eklenecek.BitisTarihi,
                            ProjeId = proje.Id,
                            AtananId = atananId
                        };
                        await gorevService.CreateGorevAsync(yeniGorev, proje.EkipId);
                        _logger.LogInformation($"'{yazan.Email}': yeni görev eklendi: '{eklenecek.Baslik}' (proje: '{proje.Ad}'{(atananId.HasValue ? ", atandı" : "")})");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"'{yazan.Email}': '{eklenecek.Baslik}' görevi eklenemedi: {ex.Message}");
                    }
                }

                foreach (var iptal in projePlani.IptalEdilenGorevler)
                {
                    try
                    {
                        var resultIptal = await gorevService.GorevIptalEtByBaslikAsync(iptal, proje.Id, yazan.Id, yazan.Rol.ToString());
                        if (resultIptal.IsSuccess)
                            _logger.LogInformation($"'{yazan.Email}': görev iptal edildi: '{iptal}'");
                        else
                            _logger.LogWarning($"'{yazan.Email}': '{iptal}' görevi iptal edilemedi: {resultIptal.Error}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"'{yazan.Email}': '{iptal}' görevi iptal edilemedi: {ex.Message}");
                    }
                }

                foreach (var guncelleme in projePlani.GuncellenecekGorevler)
                {
                    try
                    {
                        if (Enum.TryParse<GorevDurumu>(guncelleme.YeniDurum, true, out var yeniDurumEnum))
                        {
                            var resultUpdate = await gorevService.UpdateGorevDurumByBaslikAsync(
                                guncelleme.GorevBasligi, yeniDurumEnum, guncelleme.Not, yazan.Id, yazan.EkipId, yazan.Rol.ToString(), textBody);
                            if (resultUpdate.IsSuccess)
                                _logger.LogInformation($"'{yazan.Email}': durum güncellendi: '{guncelleme.GorevBasligi}' -> {yeniDurumEnum}");
                            else
                                //Başarısız sonuç (ör. "Görev bulunamadı") önceden hiç loglanmıyordu; bu, AI'ın
                                //bir görevi güncellemeyi denediği ama başlık eşleşmediği durumları sessizce
                                //yutuyordu — kullanıcıya "mail işlendi ama görev değişmedi" gibi görünüyordu
                                _logger.LogWarning($"'{yazan.Email}': '{guncelleme.GorevBasligi}' durumu güncellenemedi: {resultUpdate.Error}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"'{yazan.Email}': '{guncelleme.GorevBasligi}' durumu güncellenemedi: {ex.Message}");
                    }
                }

                foreach (var duzenleme in projePlani.DuzenlenecekGorevler)
                {
                    try
                    {
                        var resultDuzenle = await gorevService.GorevAlanlariniGuncelleByBaslikAsync(
                            duzenleme.GorevBasligi, proje.Id, duzenleme.YeniOncelik, duzenleme.YeniBaslangicTarihi, duzenleme.YeniBitisTarihi, yazan.Id, yazan.Rol.ToString());
                        if (resultDuzenle.IsSuccess)
                            _logger.LogInformation($"'{yazan.Email}': görev düzenlendi: '{duzenleme.GorevBasligi}'");
                        else
                            _logger.LogWarning($"'{yazan.Email}': '{duzenleme.GorevBasligi}' görevi düzenlenemedi: {resultDuzenle.Error}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"'{yazan.Email}': '{duzenleme.GorevBasligi}' görevi düzenlenemedi: {ex.Message} | İç hata: {ex.InnerException?.Message}");
                    }
                }
            }
        }

        //Ekibin su an aktif (Tamamlandi/Iptal disindaki) gorevlerini proje adina gore gruplayip AI'a
        //baglam olarak vermek icin metin haline getirir; ekip bilgisi yoksa veya gorev/proje sorgusu
        //basarisiz olursa bos dondurur (AI baglamsiz calismaya devam eder, hicbir seyi bloklamaz).
        //Ayni cagrida cikan aktif gorev listesi, EklenecekGorevler icin mukerrer-ekleme kontrolunde
        //de kullanilir (bkz. HaftalikPlaniIsleAsync) — ikinci bir DB sorgusuna gerek kalmasin diye
        private static async Task<(string Baglam, List<GorevDto> AktifGorevler)> MevcutGorevBasliklariniListeleAsync(Guid? ekipId, IGorevService gorevService, IProjeService projeService)
        {
            if (!ekipId.HasValue) return (string.Empty, new List<GorevDto>());

            var gorevlerResult = await gorevService.GetGorevlerByEkipIdAsync(ekipId.Value);
            var projelerResult = await projeService.GetProjelerByEkipIdAsync(ekipId.Value);
            if (!gorevlerResult.IsSuccess || gorevlerResult.Data == null ||
                !projelerResult.IsSuccess || projelerResult.Data == null)
                return (string.Empty, new List<GorevDto>());

            var projeAdMap = projelerResult.Data.ToDictionary(p => p.Id, p => p.Ad);

            var aktifGorevler = gorevlerResult.Data
                .Where(g => g.Durum != GorevDurumu.Tamamlandi && g.Durum != GorevDurumu.Iptal)
                .ToList();

            var gruplar = aktifGorevler
                .GroupBy(g => projeAdMap.TryGetValue(g.ProjeId, out var ad) ? ad : "Bilinmeyen")
                .Select(grp => $"{grp.Key}: " + string.Join(", ", grp.Select(g => $"\"{g.Baslik}\"")));

            return (string.Join("\n", gruplar), aktifGorevler);
        }
    }
}
