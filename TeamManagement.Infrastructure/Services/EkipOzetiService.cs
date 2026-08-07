using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TeamManagement.Application.Common.Results;
using TeamManagement.Application.Interfaces;
using TeamManagement.Domain.Enums;
using TeamManagement.Infrastructure.Persistence;

namespace TeamManagement.Infrastructure.Services
{
    //Ekibin kişi bazlı çalışma özetini ve hata/yorum verilerinden çıkarılan analizi tek bir
    //AI çağrısında üretip lidere mail olarak gönderir
    public class EkipOzetiService : IEkipOzetiService
    {
        private readonly TeamManagementAppDbContext _appContext;
        private readonly TeamManagementIdentityDbContext _identityContext;
        private readonly IAiMetinUretimService _aiMetin;
        private readonly IEmailService _emailService;
        private readonly IGizliVeriKorumaService _korumaService;
        private readonly IConfiguration _configuration;

        public EkipOzetiService(
            TeamManagementAppDbContext appContext,
            TeamManagementIdentityDbContext identityContext,
            IAiMetinUretimService aiMetin,
            IEmailService emailService,
            IGizliVeriKorumaService korumaService,
            IConfiguration configuration)
        {
            _appContext = appContext;
            _identityContext = identityContext;
            _aiMetin = aiMetin;
            _emailService = emailService;
            _korumaService = korumaService;
            _configuration = configuration;
        }

        public async Task<Result> HaftalikOzetGonderAsync(Guid ekipId)
        {
            var ekip = await _appContext.Ekipler.FindAsync(ekipId);
            if (ekip == null) return Result.Failure("Ekip bulunamadı.", 404);

            var uyeler = await _identityContext.Kullanicilar
                .Where(k => k.EkipId == ekipId)
                .ToListAsync();

            var lider = uyeler.FirstOrDefault(k => k.Rol == Rol.Lider);
            if (lider == null) return Result.Failure("Ekip lideri bulunamadı.", 404);

            //Liderin kendi anahtarı yoksa appsettings'teki ortak anahtara düşülür, böylece
            //özellik sadece mail entegrasyonunu ayarlamış liderlerle sınırlı kalmaz
            var apiKey = _korumaService.SifreCoz(lider.AiApiKeySifreli) ?? _configuration["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                return Result.Failure("Gemini API anahtarı yapılandırılmamış (ne lider ayarlarında ne appsettings'te).", 400);

            var projeIdler = await _appContext.Projeler
                .Where(p => p.EkipId == ekipId)
                .Select(p => p.Id)
                .ToListAsync();

            var gorevler = await _appContext.Gorevler
                .Where(g => projeIdler.Contains(g.ProjeId))
                .ToListAsync();

            if (gorevler.Count == 0)
                return Result.Failure("Ekipte özetlenecek görev bulunamadı.", 400);

            var gorevIdler = gorevler.Select(g => g.Id).ToList();

            var sinirTarihi = DateTime.UtcNow.AddDays(-7);
            var sonYorumlar = await _appContext.Yorumlar
                .Where(y => gorevIdler.Contains(y.GorevId) && y.OluşturulmaTarihi >= sinirTarihi)
                .ToListAsync();
            var sonHatalar = await _appContext.Hatalar
                .Where(h => gorevIdler.Contains(h.GorevId))
                .ToListAsync();

            //Kişi bazlı görev verisi metne dökülür — AI bunu okuyup doğal dilde özet yazacak
            var kisiVerisi = new StringBuilder();
            foreach (var uye in uyeler)
            {
                var kisiGorevleri = gorevler.Where(g => g.AtananId == uye.Id).ToList();
                if (kisiGorevleri.Count == 0) continue;

                kisiVerisi.AppendLine($"### {uye.Ad} {uye.Soyad}");
                foreach (var g in kisiGorevleri)
                {
                    var tarihBilgi = g.Deadline.HasValue ? $", teslim tarihi {g.Deadline:yyyy-MM-dd}" : "";
                    kisiVerisi.AppendLine($"- [{g.Durum}] {g.Baslik} (öncelik: {g.Oncelik}{tarihBilgi})");

                    var goreveAitYorumlar = sonYorumlar.Where(y => y.GorevId == g.Id);
                    foreach (var y in goreveAitYorumlar)
                        kisiVerisi.AppendLine($"    Yorum: {y.Icerik}");
                }
                kisiVerisi.AppendLine();
            }

            var sahipsizGorevSayisi = gorevler.Count(g => !g.AtananId.HasValue);
            if (sahipsizGorevSayisi > 0)
                kisiVerisi.AppendLine($"(Not: {sahipsizGorevSayisi} görev sahipsiz/atanmamış durumda.)");

            //Hata + yorum verisi kök neden/örüntü analizi için ayrı bir blokta toplanır
            var hataYorumVerisi = new StringBuilder();
            if (sonHatalar.Count == 0)
            {
                hataYorumVerisi.AppendLine("Son 7 günde kayıtlı hata bildirimi yok.");
            }
            else
            {
                foreach (var h in sonHatalar)
                {
                    var goreviBul = gorevler.FirstOrDefault(g => g.Id == h.GorevId);
                    hataYorumVerisi.AppendLine($"- [{h.Siddet}] Modül: {h.Modul} | Görev: {goreviBul?.Baslik ?? "?"} | Açıklama: {h.Aciklama}");
                }
            }

            var prompt = $@"
            Sen bir yazılım ekibi için haftalık durum raporu hazırlayan bir asistansın. Aşağıdaki verilere göre
            ekip liderine gönderilecek bir e-posta metni yaz. Türkçe, samimi ama profesyonel bir dille yaz.
            Markdown kullanma, düz metin yaz (madde işaretleri için sadece '-' kullanabilirsin).

            Rapor iki bölümden oluşmalı:

            1) ""KİŞİ BAZLI ÖZET"" başlığı altında, her ekip üyesi için ayrı bir paragraf: ne üzerinde çalışıyor,
               hangi görevleri tamamlanmış/devam ediyor/gecikmiş durumda, dikkat çeken bir yorum varsa belirt.
               Görevi olmayan üyeden bahsetme.

            2) ""HATA ANALİZİ"" başlığı altında, aşağıdaki hata kayıtlarını incele ve tekrar eden örüntüler,
               en çok sorun çıkan modül/kişi, olası kök nedenler hakkında kısa bir değerlendirme yaz. Hata yoksa
               bunu belirt ve bu bölümü kısa geç.

            EKİP: {ekip.Ad}

            KİŞİ BAZLI GÖREV VERİSİ:
            {kisiVerisi}

            SON 7 GÜNDEKİ HATA KAYITLARI:
            {hataYorumVerisi}
            ";

            var aiResult = await _aiMetin.MetinUretAsync(prompt, apiKey);
            if (!aiResult.IsSuccess || string.IsNullOrEmpty(aiResult.Data))
                return Result.Failure($"AI özet üretemedi: {aiResult.Error}");

            //Üretilen içerik, mail gönderimi başarısız olsa bile uygulama içinde kalıcı olarak arşivlenir
            _appContext.EkipOzetGecmisleri.Add(new TeamManagement.Domain.Entities.EkipOzetGecmisi
            {
                EkipId = ekipId,
                Icerik = aiResult.Data,
                OluşturulmaTarihi = DateTime.UtcNow
            });
            await _appContext.SaveChangesAsync();

            var htmlBody = $"<div style=\"font-family:sans-serif;white-space:pre-wrap;line-height:1.6;color:#1f2937\">{System.Net.WebUtility.HtmlEncode(aiResult.Data)}</div>";

            //Ortak bot hesabının appsettings'teki SMTP kimlik bilgileri geçersiz olabildiğinden, liderin
            //Ayarlar'da kayıtlı ve zaten IMAP için doğrulanmış kendi uygulama şifresi öncelikli kullanılır
            var liderUygulamaSifresi = _korumaService.SifreCoz(lider.MailUygulamaSifresiSifreli);
            var gonderSonuc = await _emailService.SendAsync(
                lider.Email,
                $"Haftalık Ekip Özeti — {ekip.Ad}",
                htmlBody,
                senderEmail: liderUygulamaSifresi != null ? lider.Email : null,
                senderPassword: liderUygulamaSifresi,
                senderName: $"{lider.Ad} {lider.Soyad}");
            if (!gonderSonuc.IsSuccess) return gonderSonuc;

            ekip.SonOzetGonderimTarihi = DateTime.UtcNow;
            await _appContext.SaveChangesAsync();

            return Result.Success();
        }

        //Daha önce üretilmiş özetleri en yeniden en eskiye listeler
        public async Task<Result<List<TeamManagement.Application.DTOs.Ozet.EkipOzetiGecmisDto>>> GetGecmisOzetlerAsync(Guid ekipId)
        {
            var gecmis = await _appContext.EkipOzetGecmisleri
                .Where(e => e.EkipId == ekipId)
                .OrderByDescending(e => e.OluşturulmaTarihi)
                .Select(e => new TeamManagement.Application.DTOs.Ozet.EkipOzetiGecmisDto
                {
                    Id = e.Id,
                    Icerik = e.Icerik,
                    OlusturulmaTarihi = e.OluşturulmaTarihi
                })
                .ToListAsync();

            return Result<List<TeamManagement.Application.DTOs.Ozet.EkipOzetiGecmisDto>>.Success(gecmis);
        }

        public async Task<Result> OzetSilAsync(Guid ozetId, Guid callerEkipId)
        {
            var ozet = await _appContext.EkipOzetGecmisleri.FindAsync(ozetId);
            if (ozet == null) return Result.Failure("Özet bulunamadı.", 404);

            //Başka ekibin özeti silinemez
            if (ozet.EkipId != callerEkipId)
                return Result.Failure("Bu özet üzerinde yetkiniz yok.", 403);

            _appContext.EkipOzetGecmisleri.Remove(ozet);
            await _appContext.SaveChangesAsync();
            return Result.Success();
        }
    }
}
