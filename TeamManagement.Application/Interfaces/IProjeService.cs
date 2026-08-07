using TeamManagement.Application.DTOs.Projeler;
using TeamManagement.Application.Common.Results;

namespace TeamManagement.Application.Interfaces
{
    public interface IProjeService
    {
        //callerEkipId: istegi yapan kullanicinin token'daki ekip kimligi; proje her zaman bu ekibe baglanir,
        //DTO uzerinden gelen EkipId istemci tarafindan degistirilebilecegi icin guvenilmez
        Task<Result<ProjeDto>> CreateProjeAsync(CreateProjeDto createProjeDto, Guid callerEkipId);
        Task<Result<List<ProjeDto>>> GetProjelerByEkipIdAsync(Guid ekipId);
        //Proje adina gore proje bulma islemi haftalik mail parser tarafindan kullanilir
        //ekipId verilirse arama sadece o ekibin projeleriyle sinirlanir (farkli ekiplerde ayni isimli proje varsa karismasin diye)
        Task<Result<ProjeDto>> GetProjeByNameAsync(string projeAdi, Guid? ekipId = null);
        Task<Result> DeleteProjeAsync(Guid projeId, Guid callerEkipId);
        Task<Result<ProjeDto>> UpdateProjeAsync(Guid projeId, UpdateProjeDto dto, Guid callerEkipId);
    }
}
