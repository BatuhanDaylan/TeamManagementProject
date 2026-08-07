
using Microsoft.EntityFrameworkCore;
using TeamManagement.Application.DTOs.Dashboard;
using TeamManagement.Application.DTOs.Gorevler;
using TeamManagement.Application.Interfaces;
using TeamManagement.Infrastructure.Persistence;
using TeamManagement.Application.Common.Results;

namespace TeamManagement.Infrastructure.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IGorevService _gorevService;
        private readonly IProjeService _projeService;
        private readonly TeamManagementIdentityDbContext _identityContext;

        public DashboardService(IGorevService gorevService, IProjeService projeService, TeamManagementIdentityDbContext identityContext)
        {
            _gorevService = gorevService;
            _projeService = projeService;
            _identityContext = identityContext;
        }

        public async Task<Result<List<UyeGorevOzetiDto>>> GetDashboardByEkipIdAsync(Guid ekipId)
        {
            // Identity db'den ekip üyelerini al
            var uyeler = await _identityContext.Kullanicilar
                .Where(u => u.EkipId == ekipId)
                .ToListAsync();

            // App db'den ekibin tüm görevlerini al
            var gorevlerResult = await _gorevService.GetGorevlerByEkipIdAsync(ekipId);
            var gorevler = gorevlerResult.IsSuccess ? gorevlerResult.Data! : new List<GorevDto>();

            // Görevin hangi projeye ait olduğu Dashboard'da gösterilebilsin diye proje adları eşlenir
            var projelerResult = await _projeService.GetProjelerByEkipIdAsync(ekipId);
            var projeDictionary = projelerResult.IsSuccess
                ? projelerResult.Data!.ToDictionary(p => p.Id, p => p.Ad)
                : new Dictionary<Guid, string>();

            foreach (var gorev in gorevler)
                gorev.ProjeAdi = projeDictionary.TryGetValue(gorev.ProjeId, out var ad) ? ad : null;

            var dashboard = new List<UyeGorevOzetiDto>();

            foreach (var uye in uyeler)
            {
                var uyeGorevleri = gorevler.Where(g => g.AtananId == uye.Id).ToList();
                dashboard.Add(new UyeGorevOzetiDto
                {
                    KullaniciId = uye.Id,
                    Ad = uye.Ad,
                    Soyad = uye.Soyad,
                    Email = uye.Email,
                    Rol = uye.Rol.ToString(),
                    AktifGorevler = uyeGorevleri
                });
            }



            return Result<List<UyeGorevOzetiDto>>.Success(dashboard);
        }
    }
}
