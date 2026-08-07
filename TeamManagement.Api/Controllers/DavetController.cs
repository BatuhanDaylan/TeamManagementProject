using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamManagement.Api.Extensions;
using TeamManagement.Application.DTOs.Davet;
using TeamManagement.Application.Interfaces;

namespace TeamManagement.Api.Controllers
{
    //Davet uç noktaları gönder listele doğrula ve kabul et işlemlerini yönetir
    [ApiController]
    [Route("api/[controller]")]
    public class DavetController : ControllerBase
    {
        //Davet iş mantığını yürüten servis
        private readonly IDavetService _davetService;

        public DavetController(IDavetService davetService)
        {
            _davetService = davetService;
        }

        //Lider bir e-posta adresine davet gönderir sadece Lider rolü erişebilir
        [HttpPost("gonder")]
        [Authorize(Policy = "EkipLideriPolicisi")]
        public async Task<IActionResult> DavetGonder([FromBody] DavetGonderDto dto)
        {
            var liderId = User.GetKullaniciId();
            if (liderId == null)
                return Unauthorized("Geçersiz kullanıcı kimliği.");

            var result = await _davetService.DavetGonderAsync(dto.Email, dto.EkipId, liderId.Value);
            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok("Davet e-postası başarıyla gönderildi.");
        }

        //Lider ekibine gönderilen davetlerin listesini görür sadece Lider rolü erişebilir
        [HttpGet("liste/{ekipId}")]
        [Authorize(Policy = "EkipLideriPolicisi")]
        public async Task<IActionResult> GetDavetler(Guid ekipId)
        {
            var callerEkipId = User.GetEkipId();
            if (callerEkipId == null || callerEkipId != ekipId)
                return Forbid();

            var result = await _davetService.GetDavetlerByEkipIdAsync(ekipId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        //Lider, henüz kullanılmamış (bekleyen veya süresi dolmuş) bir daveti siler sadece Lider rolü erişebilir
        [HttpDelete("{davetId}")]
        [Authorize(Policy = "EkipLideriPolicisi")]
        public async Task<IActionResult> DavetSil(Guid davetId)
        {
            var callerEkipId = User.GetEkipId();
            if (callerEkipId == null)
                return Forbid();

            var result = await _davetService.DavetSilAsync(davetId, callerEkipId.Value);
            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        //Token geçerliliğini doğrular frontend kayıt formunu açmadan önce çağırır giriş gerekmez
        [HttpGet("dogrula")]
        [AllowAnonymous]
        public async Task<IActionResult> DavetDogrula([FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest("Token boş olamaz.");

            var result = await _davetService.DavetDogrulaAsync(token);
            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        //Kullanıcı formu doldurup hesabını oluşturur token geçerliyse kayıt tamamlanır giriş gerekmez
        [HttpPost("kabul")]
        [AllowAnonymous]
        public async Task<IActionResult> DavetKabul([FromBody] DavetKabulDto dto)
        {
            //Zorunlu alanlar kontrol edilir
            if (string.IsNullOrWhiteSpace(dto.Token) ||
                string.IsNullOrWhiteSpace(dto.YeniSifre) ||
                string.IsNullOrWhiteSpace(dto.Ad))
                return BadRequest("Ad, şifre ve token zorunludur.");

            var result = await _davetService.DavetKabulEtAsync(dto);
            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok("Hesabınız başarıyla oluşturuldu. Giriş yapabilirsiniz.");
        }
    }
}
