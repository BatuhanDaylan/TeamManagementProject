using Microsoft.EntityFrameworkCore;
using TeamManagement.Application.DTOs.Yorum;
using TeamManagement.Application.Interfaces;
using TeamManagement.Domain.Entities;
using TeamManagement.Infrastructure.Persistence;
using TeamManagement.Application.Common.Results;

namespace TeamManagement.Infrastructure.Services
{
    //Yorum servisi görev yorumlarının eklenmesi ve listelenmesini yönetir
    public class YorumService : IYorumService
    {
        //Yorum ve görev verilerinin tutulduğu uygulama veritabanı bağlamı
        private readonly TeamManagementAppDbContext _appContext;

        //Yazan adı/soyadı identity db'de tutulduğu için ayrı bir context gerekir
        private readonly TeamManagementIdentityDbContext _identityContext;

        //Yorum eklendiğinde görev sahibine bildirim ulaştırmak için
        private readonly IBildirimService _bildirimService;
        private readonly INotificationService _notificationService;

        public YorumService(
            TeamManagementAppDbContext appContext,
            TeamManagementIdentityDbContext identityContext,
            IBildirimService bildirimService,
            INotificationService notificationService)
        {
            _appContext = appContext;
            _identityContext = identityContext;
            _bildirimService = bildirimService;
            _notificationService = notificationService;
        }

        //Göreve yorum ekler yazanın kimliği token dan controller katmanında alınır
        public async Task<Result<YorumDto>> YorumEkleAsync(YorumOlusturDto dto, Guid yazanId, Guid callerEkipId)
        {
            if (!await GorevEkibeAitMiAsync(dto.GorevId, callerEkipId))
                return Result<YorumDto>.Failure("Görev bulunamadı veya bu ekibe ait değil.", 403);

            var yorum = new Yorum
            {
                GorevId = dto.GorevId,
                YazanId = yazanId,
                Icerik = dto.Icerik,
                OluşturulmaTarihi = DateTime.UtcNow
            };

            _appContext.Yorumlar.Add(yorum);
            await _appContext.SaveChangesAsync();

            var yazan = await _identityContext.Kullanicilar.FindAsync(yazanId);

            var yorumDto = new YorumDto
            {
                Id = yorum.Id,
                GorevId = yorum.GorevId,
                YazanId = yorum.YazanId,
                YazanAdSoyad = yazan != null ? $"{yazan.Ad} {yazan.Soyad}" : "Bilinmeyen Kullanıcı",
                Icerik = yorum.Icerik,
                Duzenlendi = yorum.Duzenlendi,
                OlusturulmaTarihi = yorum.OluşturulmaTarihi
            };

            //Görev sahibine (kendi yorumu hariç) yeni yorum bildirimi gönderilir
            var gorev = await _appContext.Gorevler.FindAsync(dto.GorevId);
            if (gorev?.AtananId != null && gorev.AtananId != yazanId)
            {
                await _bildirimService.BildirimOlusturAsync(gorev.AtananId.Value, $"'{gorev.Baslik}' görevine yeni bir yorum eklendi.", gorev.Id);
                await _notificationService.BildirimGonderAsync(gorev.AtananId.Value, $"'{gorev.Baslik}' görevine yeni bir yorum eklendi.");
            }

            //Görevin projesindeki tüm bağlı istemcilere anlık olarak yeni yorum yayınlanır
            if (gorev != null)
                await _notificationService.YorumEklendiAsync(gorev.ProjeId, yorumDto);

            return Result<YorumDto>.Success(yorumDto);
        }

        //Belirli bir göreve ait yorumları eklenme sırasına göre listeler en eski en üstte
        public async Task<Result<List<YorumDto>>> GetYorumlarByGorevIdAsync(Guid gorevId, Guid callerEkipId)
        {
            if (!await GorevEkibeAitMiAsync(gorevId, callerEkipId))
                return Result<List<YorumDto>>.Failure("Görev bulunamadı veya bu ekibe ait değil.", 403);

            var yorumlar = await _appContext.Yorumlar
                .Where(y => y.GorevId == gorevId)
                .OrderBy(y => y.OluşturulmaTarihi)
                .ToListAsync();

            //Yazan adı/soyadı identity db'de olduğu için isimler ayrı bir sorguyla toplu çekilir
            var yazanIdler = yorumlar.Select(y => y.YazanId).Distinct().ToList();
            var yazanlar = await _identityContext.Kullanicilar
                .Where(k => yazanIdler.Contains(k.Id))
                .ToDictionaryAsync(k => k.Id, k => $"{k.Ad} {k.Soyad}");

            var sonuc = yorumlar.Select(y => new YorumDto
            {
                Id = y.Id,
                GorevId = y.GorevId,
                YazanId = y.YazanId,
                YazanAdSoyad = yazanlar.TryGetValue(y.YazanId, out var ad) ? ad : "Bilinmeyen Kullanıcı",
                Icerik = y.Icerik,
                Duzenlendi = y.Duzenlendi,
                OlusturulmaTarihi = y.OluşturulmaTarihi
            }).ToList();

            return Result<List<YorumDto>>.Success(sonuc);
        }

        //Sadece yorumu yazan kişi düzenleyebilir; başkasının yorumu değiştirilemez
        public async Task<Result<YorumDto>> YorumDuzenleAsync(Guid yorumId, string yeniIcerik, Guid callerId)
        {
            var yorum = await _appContext.Yorumlar.FindAsync(yorumId);
            if (yorum == null) return Result<YorumDto>.Failure("Yorum bulunamadı.", 404);
            if (yorum.YazanId != callerId) return Result<YorumDto>.Failure("Sadece kendi yorumunuzu düzenleyebilirsiniz.", 403);

            yorum.Icerik = yeniIcerik;
            yorum.Duzenlendi = true;
            await _appContext.SaveChangesAsync();

            var yazan = await _identityContext.Kullanicilar.FindAsync(yorum.YazanId);

            return Result<YorumDto>.Success(new YorumDto
            {
                Id = yorum.Id,
                GorevId = yorum.GorevId,
                YazanId = yorum.YazanId,
                YazanAdSoyad = yazan != null ? $"{yazan.Ad} {yazan.Soyad}" : "Bilinmeyen Kullanıcı",
                Icerik = yorum.Icerik,
                Duzenlendi = yorum.Duzenlendi,
                OlusturulmaTarihi = yorum.OluşturulmaTarihi
            });
        }

        //Sadece yorumu yazan kişi silebilir
        public async Task<Result> YorumSilAsync(Guid yorumId, Guid callerId)
        {
            var yorum = await _appContext.Yorumlar.FindAsync(yorumId);
            if (yorum == null) return Result.Failure("Yorum bulunamadı.", 404);
            if (yorum.YazanId != callerId) return Result.Failure("Sadece kendi yorumunuzu silebilirsiniz.", 403);

            _appContext.Yorumlar.Remove(yorum);
            await _appContext.SaveChangesAsync();

            return Result.Success();
        }

        //Görevin, verilen ekibin bir projesine ait olup olmadığını kontrol eder (sahiplik doğrulaması için)
        private async Task<bool> GorevEkibeAitMiAsync(Guid gorevId, Guid ekipId)
        {
            var gorev = await _appContext.Gorevler.FindAsync(gorevId);
            if (gorev == null) return false;

            var proje = await _appContext.Projeler.FindAsync(gorev.ProjeId);
            return proje != null && proje.EkipId == ekipId;
        }
    }
}
