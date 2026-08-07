using TeamManagement.Application.DTOs.Ekipler;
using TeamManagement.Application.Common.Results;

namespace TeamManagement.Application.Interfaces
{
    public interface IEkipService
    {
        Task<Result<EkipDto>> CreateEkipAsync(CreateEkipDto createEkipDto, Guid liderId);
        Task<Result<List<EkipDto>>> GetEkiplerByKullaniciIdAsync(Guid kullaniciId);
        Task<Result> DeleteEkipAsync(Guid ekipId, Guid istekYapanKullaniciId);
        //Lider bir üyeyi ekipten çıkarır; kendisini çıkaramaz
        Task<Result> UyeCikarAsync(Guid ekipId, Guid cikarilacakUyeId, Guid liderId);
        //Üye kendi isteğiyle ekipten ayrılır; lider bu yolu kullanamaz (önce ekibi silmeli/liderliği devretmeli)
        Task<Result> EkiptenAyrilAsync(Guid kullaniciId);
        //Lider, davet/e-posta akışına gerek kalmadan bir üyeyi doğrudan kendi eliyle kaydeder
        Task<Result> UyeOlusturAsync(Guid ekipId, Guid liderId, AdminKullaniciOlusturDto dto);
    }
}
