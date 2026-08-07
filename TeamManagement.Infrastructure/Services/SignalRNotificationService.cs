using Microsoft.AspNetCore.SignalR;
using TeamManagement.Application.DTOs.Gorevler;
using TeamManagement.Application.DTOs.Yorum;
using TeamManagement.Application.Interfaces;
using TeamManagement.Application.Common.Results;

namespace TeamManagement.Infrastructure.Services
{
    //INotificationService implementasyonu Infrastructure katmanında
    //Dairesel bağımlılığı önlemek için IHubContext base Hub sınıfı kullanılır GorevHub a referans verilmez
    public class SignalRNotificationService : INotificationService
    {
        //IHubContext base Hub ile inject edilir Api katmanına bağımlılık oluşmaz
        private readonly IHubContext<Hub> _hubContext;

        public SignalRNotificationService(IHubContext<Hub> hubContext)
        {
            _hubContext = hubContext;
        }

        //Görev değişince proje grubundaki tüm bağlı istemcilere GorevGuncellendi eventi gönderilir
        public async Task<Result> GorevGuncellendiAsync(Guid projeId, GorevDto gorev)
        {
            await _hubContext.Clients
                .Group($"proje_{projeId}")
                .SendAsync("GorevGuncellendi", gorev);
            return Result.Success();
        }

        //Yorum eklenince proje grubundaki tüm bağlı istemcilere YorumEklendi eventi gönderilir
        public async Task<Result> YorumEklendiAsync(Guid projeId, YorumDto yorum)
        {
            await _hubContext.Clients
                .Group($"proje_{projeId}")
                .SendAsync("YorumEklendi", yorum);
            return Result.Success();
        }

        //Belirli kullanıcıya özel bildirim gönderir kullanıcı ID si grup adı olarak kullanılır
        public async Task<Result> BildirimGonderAsync(Guid kullaniciId, string icerik)
        {
            await _hubContext.Clients
                .Group($"kullanici_{kullaniciId}")
                .SendAsync("YeniBildirim", new { Icerik = icerik, Tarih = DateTime.UtcNow });
            return Result.Success();
        }
    }
}
