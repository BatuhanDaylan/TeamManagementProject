using Microsoft.EntityFrameworkCore;
using TeamManagement.Application.DTOs.GorevIlerleme;
using TeamManagement.Application.Interfaces;
using TeamManagement.Infrastructure.Persistence;
using TeamManagement.Application.Common.Results;

namespace TeamManagement.Infrastructure.Services
{
    public class GorevIlerlemeService : IGorevIlerlemeService
    {
        private readonly TeamManagementAppDbContext _appContext;

        public GorevIlerlemeService(TeamManagementAppDbContext appContext)
        {
            _appContext = appContext;
        }

        //Göreve ait tüm AI ilerleme kayıtlarını getirir en yeni üstte
        public async Task<Result<List<GorevIlerlemeDto>>> GetIlerlemelerByGorevIdAsync(Guid gorevId, Guid callerEkipId)
        {
            var gorev = await _appContext.Gorevler.FindAsync(gorevId);
            if (gorev == null) return Result<List<GorevIlerlemeDto>>.Failure("Görev bulunamadı.", 404);

            var proje = await _appContext.Projeler.FindAsync(gorev.ProjeId);
            if (proje == null || proje.EkipId != callerEkipId)
                return Result<List<GorevIlerlemeDto>>.Failure("Görev bulunamadı veya bu ekibe ait değil.", 403);

            var ilerlemeler = await _appContext.GorevIlerlemeleri
                .Where(gi => gi.GorevId == gorevId)
                .OrderByDescending(gi => gi.OluşturulmaTarihi)
                .Select(gi => new GorevIlerlemeDto
                {
                    Id = gi.Id,
                    TamamlananKisim = gi.TamamlananKisim,
                    KaynakEmailMetni = gi.KaynakEmailMetni,
                    GorevId = gi.GorevId,
                    EkleyenKullaniciId = gi.EkleyenKullaniciId,
                    OlusturulmaTarihi = gi.OluşturulmaTarihi
                })
                .ToListAsync();

            return Result<List<GorevIlerlemeDto>>.Success(ilerlemeler);
        }
    }
}
