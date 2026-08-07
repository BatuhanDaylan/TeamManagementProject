using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamManagement.Api.Extensions;
using TeamManagement.Application.DTOs.Auth;
using TeamManagement.Application.Interfaces;

namespace TeamManagement.Api.Controllers
{
    //Kimlik doğrulama uç noktaları giriş ve davet tabanlı kayıt işlemlerini yönetir
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        //Kayıt giriş ve token üretimi için kimlik doğrulama servisi
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        //Kayıt uç noktası Swagger da gizlenir davet sistemi hazır olana kadar doğrudan erişim kapalı
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto registerDto)
        {
            var result = await _authService.RegisterAsync(registerDto);
            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        //Giriş uç noktası başarılıysa JWT token döndürür
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            var result = await _authService.LoginAsync(loginDto);
            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        //Şifre sıfırlama talebi; e-posta kayıtlı olsun ya da olmasın her zaman başarılı döner
        [HttpPost("sifre-sifirlama-talep")]
        public async Task<IActionResult> SifreSifirlamaTalep([FromBody] SifreSifirlamaTalepDto dto)
        {
            var result = await _authService.SifreSifirlamaTalepEtAsync(dto.Email);
            return Ok(result);
        }

        //Token doğrulanıp yeni şifre kaydedilir
        [HttpPost("sifre-sifirla")]
        public async Task<IActionResult> SifreSifirla([FromBody] SifreSifirlaDto dto)
        {
            var result = await _authService.SifreSifirlaAsync(dto);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        //Giriş yapan kullanıcı kendi ad/soyad bilgisini günceller
        [Authorize]
        [HttpPut("profil")]
        public async Task<IActionResult> ProfilGuncelle([FromBody] GuncelleProfilDto dto)
        {
            var kullaniciId = User.GetKullaniciId();
            if (kullaniciId == null) return Unauthorized("Geçersiz kullanıcı kimliği.");

            var result = await _authService.ProfilGuncelleAsync(kullaniciId.Value, dto);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        //Giriş yapan kullanıcı mevcut şifresini doğrulayıp yenisini belirler
        [Authorize]
        [HttpPut("sifre-degistir")]
        public async Task<IActionResult> SifreDegistir([FromBody] SifreDegistirDto dto)
        {
            var kullaniciId = User.GetKullaniciId();
            if (kullaniciId == null) return Unauthorized("Geçersiz kullanıcı kimliği.");

            var result = await _authService.SifreDegistirAsync(kullaniciId.Value, dto);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}
