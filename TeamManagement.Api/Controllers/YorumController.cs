using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamManagement.Api.Extensions;
using TeamManagement.Application.DTOs.Yorum;
using TeamManagement.Application.Interfaces;

namespace TeamManagement.Api.Controllers
{
    //Yorum uç noktaları ekle ve listele işlemlerini yönetir giriş zorunludur
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class YorumController : ControllerBase
    {
        //Yorum iş mantığını yürüten servis
        private readonly IYorumService _yorumService;

        public YorumController(IYorumService yorumService)
        {
            _yorumService = yorumService;
        }

        //Göreve yorum ekler yazan bilgisi token dan okunur istemci göndermez
        [HttpPost("ekle")]
        public async Task<IActionResult> YorumEkle([FromBody] YorumOlusturDto dto)
        {
            var yazanId = User.GetKullaniciId();
            if (yazanId == null) return Unauthorized("Geçersiz kullanıcı kimliği.");

            var callerEkipId = User.GetEkipId();
            if (callerEkipId == null) return Unauthorized("Geçersiz kullanıcı kimliği.");

            var result = await _yorumService.YorumEkleAsync(dto, yazanId.Value, callerEkipId.Value);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        //Belirli bir göreve ait tüm yorumları zamana göre sıralı listeler
        [HttpGet("gorev/{gorevId}")]
        public async Task<IActionResult> GetYorumlar(Guid gorevId)
        {
            var callerEkipId = User.GetEkipId();
            if (callerEkipId == null) return Unauthorized("Geçersiz kullanıcı kimliği.");

            var result = await _yorumService.GetYorumlarByGorevIdAsync(gorevId, callerEkipId.Value);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        //Sadece yorumu yazan kişi düzenleyebilir
        [HttpPut("{yorumId}")]
        public async Task<IActionResult> YorumDuzenle(Guid yorumId, [FromBody] UpdateYorumDto dto)
        {
            var callerId = User.GetKullaniciId();
            if (callerId == null) return Unauthorized("Geçersiz kullanıcı kimliği.");

            var result = await _yorumService.YorumDuzenleAsync(yorumId, dto.Icerik, callerId.Value);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        //Sadece yorumu yazan kişi silebilir
        [HttpDelete("{yorumId}")]
        public async Task<IActionResult> YorumSil(Guid yorumId)
        {
            var callerId = User.GetKullaniciId();
            if (callerId == null) return Unauthorized("Geçersiz kullanıcı kimliği.");

            var result = await _yorumService.YorumSilAsync(yorumId, callerId.Value);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}
