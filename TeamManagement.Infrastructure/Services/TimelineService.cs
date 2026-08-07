using Microsoft.EntityFrameworkCore;
using TeamManagement.Application.DTOs.Timeline;
using TeamManagement.Application.DTOs.Gorevler;
using TeamManagement.Application.DTOs.Projeler;
using TeamManagement.Application.Interfaces;
using TeamManagement.Infrastructure.Persistence;
using TeamManagement.Application.Common.Results;

namespace TeamManagement.Infrastructure.Services
{
    public class TimelineService : ITimelineService
    {
        private readonly IGorevService _gorevService;
        private readonly IProjeService _projeService;
        private readonly TeamManagementIdentityDbContext _identityContext;

        public TimelineService(IGorevService gorevService, IProjeService projeService, TeamManagementIdentityDbContext identityContext)
        {
            _gorevService = gorevService;
            _projeService = projeService;
            _identityContext = identityContext;
        }

        public async Task<Result<List<TimelineGorevDto>>> GetTimelineByEkipIdAsync(Guid ekipId)
        {
            // Identity db'den ekip üyelerini al (isim eşleştirmesi için)
            var uyeler = await _identityContext.Kullanicilar
                .Where(u => u.EkipId == ekipId)
                .ToDictionaryAsync(u => u.Id, u => $"{u.Ad} {u.Soyad}");

            // Projeleri al (proje adı eşleştirmesi için)
            var projelerResult = await _projeService.GetProjelerByEkipIdAsync(ekipId);
            var projeler = projelerResult.IsSuccess ? projelerResult.Data! : new List<ProjeDto>();
            var projeDictionary = projeler.ToDictionary(p => p.Id, p => p.Ad);

            // Sadece başlangıç veya bitiş tarihi olan veya planlanmış görevleri getir
            var gorevlerResult = await _gorevService.GetGorevlerByEkipIdAsync(ekipId);
            var gorevler = gorevlerResult.IsSuccess ? gorevlerResult.Data! : new List<GorevDto>();

            var timelineGorevler = gorevler
                .Where(g => g.BaslangicTarihi.HasValue || g.Deadline.HasValue)
                .Select(g => new TimelineGorevDto
                {
                    Id = g.Id,
                    Baslik = g.Baslik,
                    ProjeId = g.ProjeId,
                    ProjeAdi = projeDictionary.ContainsKey(g.ProjeId) ? projeDictionary[g.ProjeId] : "Bilinmeyen Proje",
                    AtananId = g.AtananId,
                    AtananAdSoyad = g.AtananId.HasValue && uyeler.ContainsKey(g.AtananId.Value)
                        ? uyeler[g.AtananId.Value]
                        : "Atanmamış",
                    BaslangicTarihi = g.BaslangicTarihi,
                    Deadline = g.Deadline,
                    OlusturulmaTarihi = g.OluşturulmaTarihi,
                    Durum = g.Durum,
                    Oncelik = g.Oncelik
                })
                .ToList();

            return Result<List<TimelineGorevDto>>.Success(timelineGorevler);
        }
    }
}
