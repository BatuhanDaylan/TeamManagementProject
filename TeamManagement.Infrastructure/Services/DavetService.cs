using Microsoft.EntityFrameworkCore;
using TeamManagement.Application.DTOs.Davet;
using TeamManagement.Application.Interfaces;
using TeamManagement.Domain.Entities;
using TeamManagement.Domain.Enums;
using TeamManagement.Infrastructure.Persistence;
using TeamManagement.Application.Common.Results;

namespace TeamManagement.Infrastructure.Services
{
    //Davet servisi token üretimi doğrulama kabul ve e-posta gönderimi işlemlerini yönetir
    public class DavetService : IDavetService
    {
        //Davet ve ekip verilerinin tutulduğu uygulama veritabanı
        private readonly TeamManagementAppDbContext _appContext;

        //Kullanıcı kayıt işlemi için identity veritabanı
        private readonly TeamManagementIdentityDbContext _identityContext;

        //Davet e-postası göndermek için e-posta servisi
        private readonly IEmailService _emailService;

        //Liderin Ayarlar'da kayıtlı uygulama şifresini çözmek için
        private readonly IGizliVeriKorumaService _korumaService;

        //Davet kabul edilince ekip liderine bildirim ulaştırmak için
        private readonly IBildirimService _bildirimService;
        private readonly INotificationService _notificationService;

        public DavetService(
            TeamManagementAppDbContext appContext,
            TeamManagementIdentityDbContext identityContext,
            IEmailService emailService,
            IGizliVeriKorumaService korumaService,
            IBildirimService bildirimService,
            INotificationService notificationService)
        {
            _appContext = appContext;
            _identityContext = identityContext;
            _emailService = emailService;
            _korumaService = korumaService;
            _bildirimService = bildirimService;
            _notificationService = notificationService;
        }

        //Belirtilen e-posta adresine ekip daveti gönderir token üretir ve kaydeder
        public async Task<Result<bool>> DavetGonderAsync(string email, Guid ekipId, Guid liderId)
        {
            //Ekip var mı ve gönderen gerçekten o ekibin lideri mi kontrol et
            var ekip = await _appContext.Ekipler.FindAsync(ekipId);
            if (ekip == null || ekip.OlusturanLiderId != liderId)
                return Result<bool>.Failure("Ekip bulunamadı veya yetkiniz yok.", 403);

            //Aynı adrese geçerli aktif davet zaten varsa tekrar gönderme
            var aktifDavet = await _appContext.Davetler.AnyAsync(d =>
                d.Email == email &&
                d.EkipId == ekipId &&
                !d.Kullanildi &&
                d.SonKullanmaTarihi > DateTime.UtcNow); 

            if (aktifDavet)
                return Result<bool>.Failure("Bu kullanıcıya zaten aktif bir davet gönderilmiş.");

            //Kullanıcı hâlâ bir ekibe bağlıysa davet gönderme; ekipten çıkarılmış (EkipId=null)
            //bir hesap sistemde e-postasıyla kayıtlı kalmaya devam ettiği için sadece e-postaya
            //bakmak, çıkarılan bir üyenin bir daha asla tekrar davet edilememesine yol açıyordu
            var mevcutKullanici = await _identityContext.Kullanicilar
                .AnyAsync(k => k.Email == email && k.EkipId != null);
            if (mevcutKullanici)
                return Result<bool>.Failure("Kullanıcı zaten sistemde kayıtlı.");

            //URL güvenli Base64 formatında rastgele token üret
            var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("+", "-").Replace("/", "_").Replace("=", "");

            var davet = new Davet
            {
                Email = email,
                Token = token,
                EkipId = ekipId,
                //Token 7 gün geçerlidir
                SonKullanmaTarihi = DateTime.UtcNow.AddDays(7),
                Kullanildi = false
            };

            _appContext.Davetler.Add(davet);
            await _appContext.SaveChangesAsync();

            //Kullanıcının tıklayacağı davet linki frontend adresini kullanır
            var davetLinki = $"http://localhost:5173/davet-kabul?token={token}";
            var htmlBody = $"""
                <h2>Prizma Pano'ya Davet Edildiniz</h2>
                <p>Ekip: <strong>{ekip.Ad}</strong></p>
                <p>Aşağıdaki bağlantıya tıklayarak hesabınızı oluşturabilirsiniz.
                   Bu bağlantı <strong>7 gün</strong> geçerlidir.</p>
                <a href="{davetLinki}" style="
                    display: inline-block;
                    padding: 12px 24px;
                    background-color: #6366f1;
                    color: white;
                    text-decoration: none;
                    border-radius: 6px;
                    font-weight: bold;">
                    Daveti Kabul Et
                </a>
                <p style="color: #888; font-size: 12px; margin-top: 24px;">
                    Bu daveti siz talep etmediyseniz, e-postayı görmezden gelebilirsiniz.
                </p>
                """;

            //Lider kendi Ayarlar sayfasında bir uygulama şifresi kaydetmişse davet o hesaptan gönderilir,
            //aksi halde appsettings'teki ortak bot hesabına düşülür
            var lider = await _identityContext.Kullanicilar.FindAsync(liderId);
            var liderUygulamaSifresi = _korumaService.SifreCoz(lider?.MailUygulamaSifresiSifreli);
            string? gonderenEmail = null, gonderenSifre = null, gonderenAd = null;
            if (lider != null && !string.IsNullOrEmpty(liderUygulamaSifresi))
            {
                gonderenEmail = lider.Email;
                gonderenSifre = liderUygulamaSifresi;
                gonderenAd = $"{lider.Ad} {lider.Soyad}";
            }

            var emailResult = await _emailService.SendAsync(email, "Prizma Pano — Ekip Daveti", htmlBody,
                gonderenEmail, gonderenSifre, gonderenAd);

            if (!emailResult.IsSuccess)
            {
                // E-posta gönderilemezse daveti veritabanından geri alıyoruz
                _appContext.Davetler.Remove(davet);
                await _appContext.SaveChangesAsync();
                return Result<bool>.Failure($"E-posta gönderimi başarısız: {emailResult.Error}");
            }

            return Result<bool>.Success(true);
        }

        //Davet tokenını doğrular süresi dolmuş veya kullanılmış tokenlara erişim engellenir
        public async Task<Result<DavetDogrulamaDto>> DavetDogrulaAsync(string token)
        {
            var davet = await _appContext.Davetler
                .Include(d => d.Ekip)
                .FirstOrDefaultAsync(d => d.Token == token);

            if (davet == null)
                return Result<DavetDogrulamaDto>.Failure("Davet bulunamadı.", 404);

            //Daha önce kullanılmış token ikinci kez kullanılamaz
            if (davet.Kullanildi)
                return Result<DavetDogrulamaDto>.Failure("Bu davet zaten kullanılmış.");

            //Süresi dolmuş token geçersiz sayılır
            if (davet.SonKullanmaTarihi < DateTime.UtcNow)
                return Result<DavetDogrulamaDto>.Failure("Davet süresi dolmuş.");

            var dogrulamaDto = new DavetDogrulamaDto
            {
                GecerliMi = true,
                Email = davet.Email,
                EkipId = davet.EkipId,
                Mesaj = "Davet geçerli."
            };
            return Result<DavetDogrulamaDto>.Success(dogrulamaDto);
        }

        //Daveti kabul eder yeni kullanıcı oluşturur ekibe ekler ve tokeni kullanıldı işaretler
        public async Task<Result<bool>> DavetKabulEtAsync(DavetKabulDto kabulDto)
        {
            //Önce token geçerliliğini tekrar doğrula
            var dogrulama = await DavetDogrulaAsync(kabulDto.Token);
            if (!dogrulama.IsSuccess || !dogrulama.Data!.GecerliMi)
                return Result<bool>.Failure(dogrulama.Error);

            var davet = await _appContext.Davetler
                .FirstOrDefaultAsync(d => d.Token == kabulDto.Token);

            //Daha önce ekipten çıkarılmış (EkipId=null) bir hesap aynı e-postayla tekrar davet
            //edilmiş olabilir (bkz. DavetGonderAsync'teki not); bu durumda yeni bir kayıt açmak
            //yerine mevcut hesap yeni ekibe yeniden bağlanır, aksi halde e-posta benzersizlik
            //kısıtına takılıp DbUpdateException'a düşerdi
            var mevcutKullanici = await _identityContext.Kullanicilar
                .FirstOrDefaultAsync(k => k.Email == davet!.Email);

            Kullanici yeniKullanici;
            if (mevcutKullanici != null)
            {
                if (mevcutKullanici.EkipId != null)
                    return Result<bool>.Failure("Bu e-posta adresi için zaten bir hesap oluşturulmuş.", 400);

                mevcutKullanici.Ad = kabulDto.Ad;
                mevcutKullanici.Soyad = kabulDto.Soyad;
                mevcutKullanici.SifreHash = BCrypt.Net.BCrypt.HashPassword(kabulDto.YeniSifre);
                mevcutKullanici.Rol = Rol.Uye;
                mevcutKullanici.EkipId = davet.EkipId;
                yeniKullanici = mevcutKullanici;
            }
            else
            {
                //Kullanıcıyı Uye rolüyle oluştur ve ekibe bağla
                yeniKullanici = new Kullanici
                {
                    Ad = kabulDto.Ad,
                    Soyad = kabulDto.Soyad,
                    Email = davet.Email,
                    SifreHash = BCrypt.Net.BCrypt.HashPassword(kabulDto.YeniSifre),
                    Rol = Rol.Uye,
                    EkipId = davet.EkipId,
                    OluşturulmaTarihi = DateTime.UtcNow
                };
                _identityContext.Kullanicilar.Add(yeniKullanici);
            }

            //Token tek kullanımlık olarak işaretlenir tekrar kullanılamaz
            davet.Kullanildi = true;

            try
            {
                await _identityContext.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                //Davet gönderildikten sonra ama kabul edilmeden önce aynı e-posta başka bir yoldan
                //(ör. lider "Kullanıcı Kaydet" ile) zaten kayıt olmuşsa veritabanı kısıtı burada devreye girer
                return Result<bool>.Failure("Bu e-posta adresi için zaten bir hesap oluşturulmuş.", 400);
            }
            await _appContext.SaveChangesAsync();

            //Ekip liderine yeni bir üyenin katıldığı bildirilir
            var ekip = await _appContext.Ekipler.FindAsync(davet.EkipId);
            if (ekip != null)
            {
                var icerik = $"'{yeniKullanici.Ad} {yeniKullanici.Soyad}' ekibinize katıldı.";
                await _bildirimService.BildirimOlusturAsync(ekip.OlusturanLiderId, icerik);
                await _notificationService.BildirimGonderAsync(ekip.OlusturanLiderId, icerik);
            }

            return Result<bool>.Success(true);
        }

        //Belirli bir ekibe gönderilmiş davetleri listeler en yeni en üstte
        public async Task<Result<List<DavetListeDto>>> GetDavetlerByEkipIdAsync(Guid ekipId)
        {
            var davetler = await _appContext.Davetler
                .Where(d => d.EkipId == ekipId)
                .OrderByDescending(d => d.OluşturulmaTarihi)
                .Select(d => new DavetListeDto
                {
                    Id = d.Id,
                    Email = d.Email,
                    SonKullanmaTarihi = d.SonKullanmaTarihi,
                    Kullanildi = d.Kullanildi,
                    EkipId = d.EkipId
                })
                .ToListAsync();

            return Result<List<DavetListeDto>>.Success(davetler);
        }

        //Henüz kullanılmamış bir daveti siler; kabul edilmiş davetler geçmiş kaydı olarak korunur
        public async Task<Result<bool>> DavetSilAsync(Guid davetId, Guid callerEkipId)
        {
            var davet = await _appContext.Davetler.FindAsync(davetId);
            if (davet == null)
                return Result<bool>.Failure("Davet bulunamadı.", 404);

            if (davet.EkipId != callerEkipId)
                return Result<bool>.Failure("Bu daveti silme yetkiniz yok.", 403);

            if (davet.Kullanildi)
                return Result<bool>.Failure("Kabul edilmiş bir davet silinemez.", 400);

            _appContext.Davetler.Remove(davet);
            await _appContext.SaveChangesAsync();

            return Result<bool>.Success(true);
        }
    }
}
