using TeamManagement.Application.DTOs.Bildirim;
using TeamManagement.Application.Common.Results;

namespace TeamManagement.Application.Interfaces
{
    public interface IBildirimService
    {
        //gorevId verilirse bildirim ilgili göreve bağlanır, tıklandığında oraya gidilebilir
        Task<Result> BildirimOlusturAsync(Guid kullaniciId, string icerik, Guid? gorevId = null);
        Task<Result<List<BildirimDto>>> GetBildirimlerAsync(Guid kullaniciId);
        Task<Result> OkunduIsaretle(Guid bildirimId, Guid kullaniciId);
        //Kullanıcının tüm okunmamış bildirimlerini tek seferde okundu işaretler
        Task<Result> TumunuOkunduIsaretleAsync(Guid kullaniciId);
        Task<Result<int>> GetOkunmamisSayisiAsync(Guid kullaniciId);
    }
}
