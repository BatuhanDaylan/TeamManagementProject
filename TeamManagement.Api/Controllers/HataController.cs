using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamManagement.Api.Extensions;
using TeamManagement.Application.DTOs.Hata;
using TeamManagement.Application.Interfaces;

namespace TeamManagement.Api.Controllers
{
    //Hata takip uç noktaları kayıt listeleme ve istatistik işlemlerini yönetir giriş zorunludur
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class HataController : ControllerBase
    {
        //Hata iş mantığını yürüten servis
        private readonly IHataService _hataService;

        public HataController(IHataService hataService)
        {
            _hataService = hataService;
        }

        //Yeni hata kaydeder sorumlu olarak giriş yapan kullanıcının ID si atanır
        [HttpPost("kaydet")]
        public async Task<IActionResult> HataKaydet([FromBody] HataOlusturDto dto)
        {
            var sorumluId = User.GetKullaniciId();
            if (sorumluId == null) return Unauthorized("Geçersiz kullanıcı kimliği.");

            var callerEkipId = User.GetEkipId();
            if (callerEkipId == null) return Unauthorized("Geçersiz kullanıcı kimliği.");

            var result = await _hataService.HataKaydetAsync(dto, sorumluId.Value, callerEkipId.Value);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        //Göreve ait tüm hataları listeler hem lider hem üye erişebilir
        [HttpGet("gorev/{gorevId}")]
        public async Task<IActionResult> GetHatalarByGorev(Guid gorevId)
        {
            var callerEkipId = User.GetEkipId();
            if (callerEkipId == null) return Unauthorized("Geçersiz kullanıcı kimliği.");

            var result = await _hataService.GetHatalarByGorevIdAsync(gorevId, callerEkipId.Value);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        //Hatayı Açık/Çözüldü arasında geçirir, hem lider hem üye kapatabilir
        [HttpPatch("{hataId}/durum")]
        public async Task<IActionResult> HataDurumGuncelle(Guid hataId, [FromBody] UpdateHataDurumDto dto)
        {
            var callerEkipId = User.GetEkipId();
            if (callerEkipId == null) return Unauthorized("Geçersiz kullanıcı kimliği.");

            var result = await _hataService.HataDurumGuncelleAsync(hataId, dto.YeniDurum, callerEkipId.Value);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        //Modül bazlı hata istatistiği hangi modülde kaç hata var sadece lider görebilir
        [HttpGet("istatistik/modul/{ekipId}")]
        [Authorize(Policy = "EkipLideriPolicisi")]
        public async Task<IActionResult> GetModulIstatistik(Guid ekipId)
        {
            var callerEkipId = User.GetEkipId();
            if (callerEkipId == null || callerEkipId != ekipId)
                return Forbid();

            var result = await _hataService.GetModulBazliIstatistikAsync(ekipId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        //Kişi bazlı hata istatistiği hangi üye kaç hata kaydetmiş sadece lider görebilir
        [HttpGet("istatistik/kisi/{ekipId}")]
        [Authorize(Policy = "EkipLideriPolicisi")]
        public async Task<IActionResult> GetKisiIstatistik(Guid ekipId)
        {
            var callerEkipId = User.GetEkipId();
            if (callerEkipId == null || callerEkipId != ekipId)
                return Forbid();

            var result = await _hataService.GetKisiBazliIstatistikAsync(ekipId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}
