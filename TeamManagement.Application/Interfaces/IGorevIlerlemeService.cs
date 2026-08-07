using TeamManagement.Application.DTOs.GorevIlerleme;
using TeamManagement.Application.Common.Results;

namespace TeamManagement.Application.Interfaces
{
    public interface IGorevIlerlemeService
    {
        //Göreve ait AI ilerleme kayıtlarını getirir en yeniden en eskiye sıralı
        //callerEkipId: istegi yapanin token'daki ekip kimligi; hedef gorev baska ekibe aitse islem reddedilir
        Task<Result<List<GorevIlerlemeDto>>> GetIlerlemelerByGorevIdAsync(Guid gorevId, Guid callerEkipId);
    }
}
