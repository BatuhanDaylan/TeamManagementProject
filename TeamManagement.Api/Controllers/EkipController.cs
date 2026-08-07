using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamManagement.Api.Extensions;
using TeamManagement.Application.DTOs.Ekipler;
using TeamManagement.Application.Interfaces;

namespace TeamManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Sadece giriş yapmış (Token sahibi) kullanıcılar erişebilir
    public class EkipController : ControllerBase
    {
        private readonly IEkipService _ekipService;

        public EkipController(IEkipService ekipService)
        {
            _ekipService = ekipService;
        }

        [HttpPost("olustur")]
        public async Task<IActionResult> CreateEkip([FromBody] CreateEkipDto createEkipDto)
        {
            var userId = User.GetKullaniciId();
            if (userId == null)
            {
                return Unauthorized("Geçersiz kullanıcı kimliği.");
            }

            var result = await _ekipService.CreateEkipAsync(createEkipDto, userId.Value);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("benim-ekiplerim")]
        public async Task<IActionResult> GetBenimEkiplerim()
        {
            var userId = User.GetKullaniciId();
            if (userId == null)
            {
                return Unauthorized("Geçersiz kullanıcı kimliği.");
            }

            var result = await _ekipService.GetEkiplerByKullaniciIdAsync(userId.Value);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("sil/{ekipId}")]
        public async Task<IActionResult> DeleteEkip(Guid ekipId)
        {
            var userId = User.GetKullaniciId();
            if (userId == null)
            {
                return Unauthorized("Geçersiz kullanıcı kimliği.");
            }

            var result = await _ekipService.DeleteEkipAsync(ekipId, userId.Value);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok("Ekip başarıyla silindi.");
        }

        //Lider bir üyeyi kendi ekibinden çıkarır
        [HttpDelete("uye/{uyeId}")]
        public async Task<IActionResult> UyeCikar(Guid uyeId)
        {
            var liderId = User.GetKullaniciId();
            var ekipId = User.GetEkipId();
            if (liderId == null || ekipId == null) return Unauthorized("Geçersiz kullanıcı kimliği.");

            var result = await _ekipService.UyeCikarAsync(ekipId.Value, uyeId, liderId.Value);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        //Lider, davet/e-posta akışına gerek kalmadan bir üyeyi doğrudan kendi eliyle kaydeder
        [HttpPost("uye-olustur")]
        public async Task<IActionResult> UyeOlustur([FromBody] AdminKullaniciOlusturDto dto)
        {
            var liderId = User.GetKullaniciId();
            var ekipId = User.GetEkipId();
            if (liderId == null || ekipId == null) return Unauthorized("Geçersiz kullanıcı kimliği.");

            var result = await _ekipService.UyeOlusturAsync(ekipId.Value, liderId.Value, dto);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        //Giriş yapan üye kendi isteğiyle ekipten ayrılır
        [HttpPost("ayril")]
        public async Task<IActionResult> EkiptenAyril()
        {
            var kullaniciId = User.GetKullaniciId();
            if (kullaniciId == null) return Unauthorized("Geçersiz kullanıcı kimliği.");

            var result = await _ekipService.EkiptenAyrilAsync(kullaniciId.Value);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}
