using Microsoft.EntityFrameworkCore;
using TeamManagement.Application.DTOs.Ekipler;
using TeamManagement.Application.Interfaces;
using TeamManagement.Domain.Enums;
using TeamManagement.Infrastructure.Persistence;
using TeamManagement.Application.Common.Results;

namespace TeamManagement.Infrastructure.Services
{
    //Ekip yönetim servisi oluşturma listeleme ve silme işlemlerini yönetir
    public class EkipService : IEkipService
    {
        //Görev proje ekip verilerinin tutulduğu uygulama veritabanı bağlamı
        private readonly TeamManagementAppDbContext _appContext;

        //Kullanıcı kimlik ve rol bilgilerinin tutulduğu identity veritabanı bağlamı
        private readonly TeamManagementIdentityDbContext _identityContext;

        public EkipService(TeamManagementAppDbContext appContext, TeamManagementIdentityDbContext identityContext)
        {
            _appContext = appContext;
            _identityContext = identityContext;
        }

        //Yeni ekip oluşturur ve kurucuyu Lider rolüne yükseltir
        public async Task<Result<EkipDto>> CreateEkipAsync(CreateEkipDto createEkipDto, Guid liderId)
        {
            //Kullanıcı zaten bir ekibe bağlıysa yeni ekip kurarak mevcut ekibinden kopmasını engelle
            var mevcutKullanici = await _identityContext.Kullanicilar.FindAsync(liderId);
            if (mevcutKullanici == null)
                return Result<EkipDto>.Failure("Kullanıcı bulunamadı.", 404);
            if (mevcutKullanici.EkipId != null)
                return Result<EkipDto>.Failure("Zaten bir ekibe bağlısınız, yeni ekip kuramazsınız.", 400);

            //Ekibi uygulama veritabanına kaydet
            var yeniEkip = new TeamManagement.Domain.Entities.Ekip
            {
                Id = Guid.NewGuid(),
                Ad = createEkipDto.Ad,
                OlusturanLiderId = liderId,
                OluşturulmaTarihi = DateTime.UtcNow
            };

            _appContext.Ekipler.Add(yeniEkip);
            await _appContext.SaveChangesAsync();

            //Kurucunun rolünü Identity veritabanında Lider olarak güncelle
            mevcutKullanici.EkipId = yeniEkip.Id;
            mevcutKullanici.Rol = Rol.Lider;
            await _identityContext.SaveChangesAsync();

            var dto = new EkipDto
            {
                Id = yeniEkip.Id,
                Ad = yeniEkip.Ad,
                OlusturanLiderId = yeniEkip.OlusturanLiderId,
                OluşturulmaTarihi = yeniEkip.OluşturulmaTarihi
            };

            return Result<EkipDto>.Success(dto);
        }

        //Kullanıcının üyesi olduğu ekibi listeler (kurucu olsun ya da davetle katılmış olsun fark etmez)
        //Not: veri modeli kullanıcı başına tek ekip (Kullanici.EkipId) destekliyor, bu yüzden en fazla 1 kayıt döner
        public async Task<Result<List<EkipDto>>> GetEkiplerByKullaniciIdAsync(Guid kullaniciId)
        {
            var kullanici = await _identityContext.Kullanicilar.FindAsync(kullaniciId);
            if (kullanici?.EkipId == null)
                return Result<List<EkipDto>>.Success(new List<EkipDto>());

            var ekipler = await _appContext.Ekipler
                .Where(e => e.Id == kullanici.EkipId)
                .Select(e => new EkipDto
                {
                    Id = e.Id,
                    Ad = e.Ad,
                    OlusturanLiderId = e.OlusturanLiderId,
                    OluşturulmaTarihi = e.OluşturulmaTarihi
                })
                .ToListAsync();

            return Result<List<EkipDto>>.Success(ekipler);
        }

        //Ekibi siler işlemi yapan kişi ekibin kurucusu değilse false döner
        public async Task<Result> DeleteEkipAsync(Guid ekipId, Guid istekYapanKullaniciId)
        {
            var ekip = await _appContext.Ekipler.FindAsync(ekipId);

            //Ekip bulunamazsa veya silmeye çalışan kişi kurucu değilse izin verilmez
            if (ekip == null || ekip.OlusturanLiderId != istekYapanKullaniciId)
            {
                return Result.Failure("Ekip bulunamadı veya yetkiniz yok.", 403);
            }

            //Ekip ve Kullanici ayrı veritabanlarında olduğu için aralarında FK/cascade yok;
            //ekip silinmeden önce üyelerin EkipId'si Identity DB'de elle temizlenmezse
            //var olmayan bir ekibe bağlı, sahipsiz kullanıcı kayıtları kalır
            var ekipUyeleri = await _identityContext.Kullanicilar
                .Where(k => k.EkipId == ekipId)
                .ToListAsync();

            foreach (var uye in ekipUyeleri)
            {
                uye.EkipId = null;
                uye.Rol = Rol.Uye;
            }

            //Üyeler önce serbest bırakılır; ekip silme adımı bir sebeple başarısız olursa
            //en kötü ihtimalle üyesiz bir ekip kaydı kalır, sahipsiz kullanıcı kalmaz
            await _identityContext.SaveChangesAsync();

            _appContext.Ekipler.Remove(ekip);
            await _appContext.SaveChangesAsync();

            return Result.Success();
        }

        //Lider bir üyeyi ekipten çıkarır; üyenin üstlendiği görevler sahipsiz kalır, kendisi silinmez
        public async Task<Result> UyeCikarAsync(Guid ekipId, Guid cikarilacakUyeId, Guid liderId)
        {
            var ekip = await _appContext.Ekipler.FindAsync(ekipId);
            if (ekip == null || ekip.OlusturanLiderId != liderId)
                return Result.Failure("Ekip bulunamadı veya yetkiniz yok.", 403);

            if (cikarilacakUyeId == liderId)
                return Result.Failure("Kendinizi ekipten çıkaramazsınız; ekibi silmeniz gerekir.", 400);

            var uye = await _identityContext.Kullanicilar.FindAsync(cikarilacakUyeId);
            if (uye == null || uye.EkipId != ekipId)
                return Result.Failure("Kullanıcı bu ekibe ait değil.", 404);

            await GorevleriSahipsizBirakAsync(ekipId, cikarilacakUyeId);

            uye.EkipId = null;
            uye.Rol = Rol.Uye;
            await _identityContext.SaveChangesAsync();

            return Result.Success();
        }

        //Üye kendi isteğiyle ekipten ayrılır; lider bu yolu kullanamaz
        public async Task<Result> EkiptenAyrilAsync(Guid kullaniciId)
        {
            var kullanici = await _identityContext.Kullanicilar.FindAsync(kullaniciId);
            if (kullanici == null || kullanici.EkipId == null)
                return Result.Failure("Bir ekibe üye değilsiniz.", 400);

            if (kullanici.Rol == Rol.Lider)
                return Result.Failure("Lider olarak ekipten ayrılamazsınız; önce ekibi silmeniz gerekir.", 400);

            await GorevleriSahipsizBirakAsync(kullanici.EkipId.Value, kullaniciId);

            kullanici.EkipId = null;
            await _identityContext.SaveChangesAsync();

            return Result.Success();
        }

        //Lider, davet/e-posta akışına gerek kalmadan bir üyeyi doğrudan kendi eliyle kaydeder
        //(ör. proje sadece yerel makinede çalışıp davet linki dışarıdan açılamadığında kullanılır)
        public async Task<Result> UyeOlusturAsync(Guid ekipId, Guid liderId, AdminKullaniciOlusturDto dto)
        {
            var ekip = await _appContext.Ekipler.FindAsync(ekipId);
            if (ekip == null || ekip.OlusturanLiderId != liderId)
                return Result.Failure("Ekip bulunamadı veya yetkiniz yok.", 403);

            if (string.IsNullOrWhiteSpace(dto.Ad) || string.IsNullOrWhiteSpace(dto.Soyad)
                || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Sifre))
                return Result.Failure("Ad, soyad, e-posta ve şifre alanları zorunludur.", 400);

            if (dto.Sifre.Length < 6)
                return Result.Failure("Şifre en az 6 karakter olmalıdır.", 400);

            var emailKullanimda = await _identityContext.Kullanicilar.AnyAsync(k => k.Email == dto.Email);
            if (emailKullanimda)
                return Result.Failure("Bu e-posta adresi zaten kullanılıyor.", 400);

            var yeniKullanici = new TeamManagement.Domain.Entities.Kullanici
            {
                Ad = dto.Ad.Trim(),
                Soyad = dto.Soyad.Trim(),
                Email = dto.Email.Trim(),
                SifreHash = BCrypt.Net.BCrypt.HashPassword(dto.Sifre),
                Rol = Rol.Uye,
                EkipId = ekipId,
                OluşturulmaTarihi = DateTime.UtcNow
            };

            _identityContext.Kullanicilar.Add(yeniKullanici);
            try
            {
                await _identityContext.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                //Yukarıdaki AnyAsync kontrolü ile buradaki kayıt arasında eşzamanlı bir istek (ör. çift
                //tıklama) aynı e-postayla kayıt oluşturduysa veritabanındaki benzersizlik kısıtı devreye girer
                return Result.Failure("Bu e-posta adresi zaten kullanılıyor.", 400);
            }

            return Result.Success();
        }

        //Ekipten ayrılan/çıkarılan üyenin üstlendiği görevler sahipsiz (Atanmamış) hale getirilir,
        //aksi halde artık ekipte olmayan birine atanmış "hayalet" görevler kalır
        private async Task GorevleriSahipsizBirakAsync(Guid ekipId, Guid uyeId)
        {
            var etkilenenGorevler = await _appContext.Gorevler
                .Where(g => g.AtananId == uyeId && _appContext.Projeler
                    .Where(p => p.EkipId == ekipId)
                    .Select(p => p.Id)
                    .Contains(g.ProjeId))
                .ToListAsync();

            foreach (var gorev in etkilenenGorevler)
                gorev.AtananId = null;

            if (etkilenenGorevler.Count > 0)
                await _appContext.SaveChangesAsync();
        }
    }
}
