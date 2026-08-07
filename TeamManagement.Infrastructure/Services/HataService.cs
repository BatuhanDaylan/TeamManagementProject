using Microsoft.EntityFrameworkCore;
using TeamManagement.Application.DTOs.Hata;
using TeamManagement.Application.Interfaces;
using TeamManagement.Domain.Entities;
using TeamManagement.Domain.Enums;
using TeamManagement.Infrastructure.Persistence;
using TeamManagement.Application.Common.Results;

namespace TeamManagement.Infrastructure.Services
{
    //Hata takip servisi kayıt listeleme ve modül ile kişi bazlı istatistik üretir
    public class HataService : IHataService
    {
        //Hata proje ve görev verilerinin tutulduğu uygulama veritabanı bağlamı
        private readonly TeamManagementAppDbContext _appContext;

        public HataService(TeamManagementAppDbContext appContext)
        {
            _appContext = appContext;
        }

        //Yeni hata kaydeder sorumlu kişi olarak görevin atandığı üye kaydedilir (hatayı bildiren
        //değil) — aksi halde "Kişi Bazlı Hatalar" raporu, görevi inceleyip hatayı bildiren lideri
        //hatanın sahibiymiş gibi gösteriyordu; oysa hatadan sorumlu olan görevi yapan kişidir.
        //Görev kimseye atanmamışsa (sahipsiz), gösterilecek başka bir sorumlu olmadığından
        //hatayı bildiren kullanıcı (sorumluId) yedek olarak kullanılır.
        public async Task<Result<HataDto>> HataKaydetAsync(HataOlusturDto dto, Guid sorumluId, Guid callerEkipId)
        {
            var gorev = await _appContext.Gorevler.FindAsync(dto.GorevId);
            if (gorev == null) return Result<HataDto>.Failure("Görev bulunamadı veya bu ekibe ait değil.", 403);

            var proje = await _appContext.Projeler.FindAsync(gorev.ProjeId);
            if (proje == null || proje.EkipId != callerEkipId)
                return Result<HataDto>.Failure("Görev bulunamadı veya bu ekibe ait değil.", 403);

            var hata = new Hata
            {
                GorevId = dto.GorevId,
                SorumluUyeId = gorev.AtananId ?? sorumluId,
                Modul = dto.Modul,
                Aciklama = dto.Aciklama,
                Siddet = dto.Siddet,
                OluşturulmaTarihi = DateTime.UtcNow
            };

            _appContext.Hatalar.Add(hata);
            await _appContext.SaveChangesAsync();

            var hataDto = new HataDto
            {
                Id = hata.Id,
                GorevId = hata.GorevId,
                SorumluUyeId = hata.SorumluUyeId,
                Modul = hata.Modul,
                Aciklama = hata.Aciklama,
                Siddet = hata.Siddet,
                Durum = hata.Durum,
                OlusturulmaTarihi = hata.OluşturulmaTarihi
            };

            return Result<HataDto>.Success(hataDto);
        }

        //Belirli bir göreve ait hatalar en yeni en üstte listelenir
        public async Task<Result<List<HataDto>>> GetHatalarByGorevIdAsync(Guid gorevId, Guid callerEkipId)
        {
            if (!await GorevEkibeAitMiAsync(gorevId, callerEkipId))
                return Result<List<HataDto>>.Failure("Görev bulunamadı veya bu ekibe ait değil.", 403);

            var hatalar = await _appContext.Hatalar
                .Where(h => h.GorevId == gorevId)
                .OrderByDescending(h => h.OluşturulmaTarihi)
                .Select(h => new HataDto
                {
                    Id = h.Id,
                    GorevId = h.GorevId,
                    SorumluUyeId = h.SorumluUyeId,
                    Modul = h.Modul,
                    Aciklama = h.Aciklama,
                    Siddet = h.Siddet,
                    Durum = h.Durum,
                    OlusturulmaTarihi = h.OluşturulmaTarihi
                })
                .ToListAsync();

            return Result<List<HataDto>>.Success(hatalar);
        }

        //Modül bazlı hata istatistiği ekip proje görev hata zinciriyle filtreleyerek gruplar
        public async Task<Result<List<HataIstatistikDto>>> GetModulBazliIstatistikAsync(Guid ekipId)
        {
            //Ekibe ait projelerin görevlerindeki hatalar modüle göre gruplanır
            var istatistikler = await _appContext.Hatalar
                .Where(h => _appContext.Gorevler
                    .Where(g => _appContext.Projeler
                        .Where(p => p.EkipId == ekipId)
                        .Select(p => p.Id)
                        .Contains(g.ProjeId))
                    .Select(g => g.Id)
                    .Contains(h.GorevId))
                .GroupBy(h => h.Modul)
                .Select(g => new HataIstatistikDto
                {
                    Modul = g.Key,
                    HataSayisi = g.Count()
                })
                //En çok hata yapan modül en üste gelsin
                .OrderByDescending(x => x.HataSayisi)
                .ToListAsync();

            return Result<List<HataIstatistikDto>>.Success(istatistikler);
        }

        //Kişi bazlı hata istatistiği hangi üyenin kaç hata kaydettiğini gösterir
        public async Task<Result<List<KisiHataIstatistikDto>>> GetKisiBazliIstatistikAsync(Guid ekipId)
        {
            //Ekibe ait projelerin görevlerindeki hatalar sorumlu kişiye göre gruplanır
            var istatistikler = await _appContext.Hatalar
                .Where(h => _appContext.Gorevler
                    .Where(g => _appContext.Projeler
                        .Where(p => p.EkipId == ekipId)
                        .Select(p => p.Id)
                        .Contains(g.ProjeId))
                    .Select(g => g.Id)
                    .Contains(h.GorevId))
                .GroupBy(h => h.SorumluUyeId)
                .Select(g => new KisiHataIstatistikDto
                {
                    KullaniciId = g.Key,
                    HataSayisi = g.Count()
                })
                //En çok hata kaydeden kişi en üste gelsin
                .OrderByDescending(x => x.HataSayisi)
                .ToListAsync();

            return Result<List<KisiHataIstatistikDto>>.Success(istatistikler);
        }

        //Hatayı Açık/Çözüldü arasında geçirir; hatanın ait olduğu görev üzerinden ekip sahipliği doğrulanır
        public async Task<Result> HataDurumGuncelleAsync(Guid hataId, HataDurumu yeniDurum, Guid callerEkipId)
        {
            var hata = await _appContext.Hatalar.FindAsync(hataId);
            if (hata == null) return Result.Failure("Hata kaydı bulunamadı.", 404);

            if (!await GorevEkibeAitMiAsync(hata.GorevId, callerEkipId))
                return Result.Failure("Bu hataya erişim yetkiniz yok.", 403);

            hata.Durum = yeniDurum;
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
