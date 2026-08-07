using TeamManagement.Application.DTOs.Yorum;
using TeamManagement.Application.Common.Results;

namespace TeamManagement.Application.Interfaces
{
    public interface IYorumService
    {
        //callerEkipId: istegi yapanin token'daki ekip kimligi; hedef gorev baska ekibe aitse islem reddedilir
        Task<Result<YorumDto>> YorumEkleAsync(YorumOlusturDto dto, Guid yazanId, Guid callerEkipId);
        Task<Result<List<YorumDto>>> GetYorumlarByGorevIdAsync(Guid gorevId, Guid callerEkipId);
        //Sadece yorumu yazan kişi düzenleyebilir/silebilir
        Task<Result<YorumDto>> YorumDuzenleAsync(Guid yorumId, string yeniIcerik, Guid callerId);
        Task<Result> YorumSilAsync(Guid yorumId, Guid callerId);
    }
}
