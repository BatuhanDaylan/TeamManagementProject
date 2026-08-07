using TeamManagement.Application.DTOs.Gorevler;
using TeamManagement.Application.DTOs.Yorum;
using TeamManagement.Application.Common.Results;

namespace TeamManagement.Application.Interfaces
{
    //SignalR üzerinden istemcilere anlık bildirim gönderme sözleşmesi
    public interface INotificationService
    {
        //Görev güncellenince ilgili projenin tüm bağlı istemcilerine yayın yap
        Task<Result> GorevGuncellendiAsync(Guid projeId, GorevDto gorev);

        //Yorum eklenince ilgili projenin tüm bağlı istemcilerine yayın yap
        Task<Result> YorumEklendiAsync(Guid projeId, YorumDto yorum);

        //Belirli bir kullanıcıya özel anlık bildirim gönder
        Task<Result> BildirimGonderAsync(Guid kullaniciId, string icerik);
    }
}
