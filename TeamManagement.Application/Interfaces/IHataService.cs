using TeamManagement.Application.DTOs.Hata;
using TeamManagement.Application.Common.Results;
using TeamManagement.Domain.Enums;

namespace TeamManagement.Application.Interfaces
{
    public interface IHataService
    {
        //callerEkipId: istegi yapanin token'daki ekip kimligi; hedef gorev baska ekibe aitse islem reddedilir
        Task<Result<HataDto>> HataKaydetAsync(HataOlusturDto dto, Guid sorumluId, Guid callerEkipId);
        Task<Result<List<HataDto>>> GetHatalarByGorevIdAsync(Guid gorevId, Guid callerEkipId);
        Task<Result<List<HataIstatistikDto>>> GetModulBazliIstatistikAsync(Guid ekipId);
        Task<Result<List<KisiHataIstatistikDto>>> GetKisiBazliIstatistikAsync(Guid ekipId);
        //Hatayı Açık/Çözüldü arasında geçirir; sadece hatanın ait olduğu ekibin üyeleri değiştirebilir
        Task<Result> HataDurumGuncelleAsync(Guid hataId, HataDurumu yeniDurum, Guid callerEkipId);
    }
}
