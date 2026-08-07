using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TeamManagement.Application.Interfaces;
using TeamManagement.Infrastructure.Persistence;

namespace TeamManagement.Infrastructure.BackgroundServices
{
    //Her ekip için haftada bir (Cuma günleri) otomatik olarak kişi bazlı özet + hata analizi
    //mailini lidere gönderir. Manuel "Özet Gönder" butonuyla aynı servisi (IEkipOzetiService) kullanır.
    public class HaftalikOzetBackgroundService : BackgroundService
    {
        private readonly ILogger<HaftalikOzetBackgroundService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public HaftalikOzetBackgroundService(ILogger<HaftalikOzetBackgroundService> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("HaftalikOzetBackgroundService başlatıldı. (Haftalık otomatik özet gönderici)");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await KontrolEtVeGonderAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Haftalık özet kontrolü sırasında hata: {ex.Message}");
                }

                //Saatlik kontrol yeterli: Cuma gününün doğru saat aralığında olup olmadığına her saat bakılır
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task KontrolEtVeGonderAsync(CancellationToken cancellationToken)
        {
            //Sadece Cuma günü belirli bir saat diliminde tetiklenir; saatlik kontrol bu pencereyi bir kez yakalar
            if (DateTime.UtcNow.DayOfWeek != DayOfWeek.Friday)
                return;

            using var scope = _scopeFactory.CreateScope();
            var appContext = scope.ServiceProvider.GetRequiredService<TeamManagementAppDbContext>();
            var ekipOzetiService = scope.ServiceProvider.GetRequiredService<IEkipOzetiService>();

            //Son 6 günden uzun süredir gönderilmemiş (ya da hiç gönderilmemiş) ekipler bu hafta için uygun sayılır
            var sinirTarihi = DateTime.UtcNow.AddDays(-6);
            var uygunEkipler = await appContext.Ekipler
                .Where(e => e.SonOzetGonderimTarihi == null || e.SonOzetGonderimTarihi < sinirTarihi)
                .ToListAsync(cancellationToken);

            foreach (var ekip in uygunEkipler)
            {
                try
                {
                    var sonuc = await ekipOzetiService.HaftalikOzetGonderAsync(ekip.Id);
                    if (sonuc.IsSuccess)
                        _logger.LogInformation($"'{ekip.Ad}' ekibine haftalık özet gönderildi.");
                    else
                        _logger.LogWarning($"'{ekip.Ad}' ekibine haftalık özet gönderilemedi: {sonuc.Error}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"'{ekip.Ad}' ekibi için haftalık özet işlenemedi: {ex.Message}");
                }
            }
        }
    }
}
