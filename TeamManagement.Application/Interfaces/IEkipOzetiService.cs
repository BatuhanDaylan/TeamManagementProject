using TeamManagement.Application.Common.Results;
using TeamManagement.Application.DTOs.Ozet;

namespace TeamManagement.Application.Interfaces
{
    public interface IEkipOzetiService
    {
        //Ekibin kişi bazlı çalışma özetini ve son dönemdeki yorum/hata verilerinden AI ile
        //çıkarılan hata analizini içeren bir mail hazırlayıp ekip liderine gönderir; üretilen
        //içerik ayrıca arşive kaydedilir
        Task<Result> HaftalikOzetGonderAsync(Guid ekipId);
        //Daha önce üretilmiş ve arşivlenmiş özetleri en yeniden en eskiye listeler
        Task<Result<List<EkipOzetiGecmisDto>>> GetGecmisOzetlerAsync(Guid ekipId);
        //Arşivdeki tek bir özeti siler; ozet baska ekibe aitse reddedilir
        Task<Result> OzetSilAsync(Guid ozetId, Guid callerEkipId);
    }
}
