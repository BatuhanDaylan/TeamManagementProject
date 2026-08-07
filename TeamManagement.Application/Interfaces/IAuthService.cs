using TeamManagement.Application.DTOs.Auth;
using TeamManagement.Application.Common.Results;

namespace TeamManagement.Application.Interfaces
{
    public interface IAuthService
    {
        Task<Result<AuthResultDto>> RegisterAsync(RegisterDto registerDto);
        Task<Result<AuthResultDto>> LoginAsync(LoginDto loginDto);
        //E-posta sistemde kayıtlı değilse bile güvenlik gereği başarılı döner (e-posta numaralandırmayı önlemek için)
        Task<Result> SifreSifirlamaTalepEtAsync(string email);
        Task<Result> SifreSifirlaAsync(SifreSifirlaDto dto);
        Task<Result<KullaniciDto>> ProfilGuncelleAsync(Guid kullaniciId, GuncelleProfilDto dto);
        //Mevcut şifre doğrulanmadan değişikliğe izin verilmez
        Task<Result> SifreDegistirAsync(Guid kullaniciId, SifreDegistirDto dto);
    }
}
