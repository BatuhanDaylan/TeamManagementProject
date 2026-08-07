using MailKit.Net.Imap;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TeamManagement.Application.Common.Results;
using TeamManagement.Application.DTOs.Ayarlar;
using TeamManagement.Application.Interfaces;
using TeamManagement.Infrastructure.Persistence;

namespace TeamManagement.Infrastructure.Services
{
    //Lider'in kendi mail/AI ayarlarını yönetir; IMAP şifresi kaydedilmeden önce canlı bağlantı testi yapılır
    public class MailAyarlariService : IMailAyarlariService
    {
        private readonly TeamManagementIdentityDbContext _identityContext;
        private readonly IGizliVeriKorumaService _korumaService;
        private readonly IConfiguration _configuration;

        public MailAyarlariService(
            TeamManagementIdentityDbContext identityContext,
            IGizliVeriKorumaService korumaService,
            IConfiguration configuration)
        {
            _identityContext = identityContext;
            _korumaService = korumaService;
            _configuration = configuration;
        }

        public async Task<Result<MailAyarlariDurumDto>> GetDurumAsync(Guid kullaniciId)
        {
            var kullanici = await _identityContext.Kullanicilar.FindAsync(kullaniciId);
            if (kullanici == null)
                return Result<MailAyarlariDurumDto>.Failure("Kullanıcı bulunamadı.", 404);

            var dto = new MailAyarlariDurumDto
            {
                MailYapilandirildiMi = !string.IsNullOrEmpty(kullanici.MailUygulamaSifresiSifreli),
                AiApiKeyYapilandirildiMi = !string.IsNullOrEmpty(kullanici.AiApiKeySifreli),
                KayitliEmail = kullanici.Email
            };
            return Result<MailAyarlariDurumDto>.Success(dto);
        }

        public async Task<Result> KaydetAsync(Guid kullaniciId, MailAyarlariDto dto)
        {
            var kullanici = await _identityContext.Kullanicilar.FindAsync(kullaniciId);
            if (kullanici == null)
                return Result.Failure("Kullanıcı bulunamadı.", 404);

            //Uygulama şifresi gönderilmişse önce canlı IMAP bağlantısı denenir, hatalıysa hiçbir şey kaydedilmez
            if (!string.IsNullOrEmpty(dto.UygulamaSifresi))
            {
                var testSonucu = await ImapBaglantisiniTestEtAsync(kullanici.Email, dto.UygulamaSifresi);
                if (!testSonucu.IsSuccess)
                    return testSonucu;

                kullanici.MailUygulamaSifresiSifreli = _korumaService.Sifrele(dto.UygulamaSifresi);
                //Kimlik bilgisi değişti, tarama sıfırdan taban alsın diye işaretçi resetlenir
                kullanici.SonIslenenMailUid = null;
                kullanici.SonIslenenMailUidValidity = null;
            }
            else if (dto.UygulamaSifresi == string.Empty)
            {
                kullanici.MailUygulamaSifresiSifreli = null;
                kullanici.SonIslenenMailUid = null;
                kullanici.SonIslenenMailUidValidity = null;
            }

            //AI API anahtarı için canlı test yapılmaz (Gemini çağrısı kota harcar), sadece şifrelenip kaydedilir
            if (!string.IsNullOrEmpty(dto.AiApiKey))
            {
                kullanici.AiApiKeySifreli = _korumaService.Sifrele(dto.AiApiKey);
            }
            else if (dto.AiApiKey == string.Empty)
            {
                kullanici.AiApiKeySifreli = null;
            }

            await _identityContext.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<Result> KaldirAsync(Guid kullaniciId)
        {
            var kullanici = await _identityContext.Kullanicilar.FindAsync(kullaniciId);
            if (kullanici == null)
                return Result.Failure("Kullanıcı bulunamadı.", 404);

            kullanici.MailUygulamaSifresiSifreli = null;
            kullanici.AiApiKeySifreli = null;
            kullanici.SonIslenenMailUid = null;
            kullanici.SonIslenenMailUidValidity = null;

            await _identityContext.SaveChangesAsync();
            return Result.Success();
        }

        //appsettings'teki global IMAP host/port/ssl ile, verilen email+şifre kombinasyonunun gerçekten
        //bağlanabildiğini doğrular; kaydetmeden önce yanlış şifre girilmesini engeller
        private async Task<Result> ImapBaglantisiniTestEtAsync(string email, string uygulamaSifresi)
        {
            var host = _configuration["EmailSettings:ImapServer"];
            var portString = _configuration["EmailSettings:ImapPort"];
            var useSslString = _configuration["EmailSettings:UseSsl"];

            if (string.IsNullOrEmpty(host))
                return Result.Failure("IMAP sunucu ayarları yapılandırılmamış.", 500);

            var port = string.IsNullOrEmpty(portString) ? 993 : int.Parse(portString);
            var useSsl = string.IsNullOrEmpty(useSslString) || bool.Parse(useSslString);

            using var client = new ImapClient();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try
            {
                await client.ConnectAsync(host, port, useSsl, cts.Token);
                await client.AuthenticateAsync(email, uygulamaSifresi, cts.Token);
                await client.DisconnectAsync(true, cts.Token);
                return Result.Success();
            }
            catch (OperationCanceledException)
            {
                return Result.Failure("Bağlantı zaman aşımına uğradı, lütfen tekrar deneyin.", 400);
            }
            catch (Exception)
            {
                return Result.Failure("E-posta veya uygulama şifresi hatalı, IMAP bağlantısı kurulamadı.", 400);
            }
        }
    }
}
