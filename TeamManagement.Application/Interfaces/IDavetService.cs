using TeamManagement.Application.DTOs.Davet;
using TeamManagement.Application.Common.Results;

namespace TeamManagement.Application.Interfaces
{
    public interface IDavetService
    {
        // Lider, bir e-posta adresine davet gönderir
        Task<Result<bool>> DavetGonderAsync(string email, Guid ekipId, Guid liderId);

        // Davet tokenını doğrular (kayıt formu açılmadan önce kontrol)
        Task<Result<DavetDogrulamaDto>> DavetDogrulaAsync(string token);

        // Kullanıcı şifresini belirler ve sisteme katılır
        Task<Result<bool>> DavetKabulEtAsync(DavetKabulDto kabulDto);

        // Lider davet listesini görür
        Task<Result<List<DavetListeDto>>> GetDavetlerByEkipIdAsync(Guid ekipId);

        // Lider henüz kullanılmamış (bekleyen veya süresi dolmuş) bir daveti siler
        Task<Result<bool>> DavetSilAsync(Guid davetId, Guid callerEkipId);
    }
}
