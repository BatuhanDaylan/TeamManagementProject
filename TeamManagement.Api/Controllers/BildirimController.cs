using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamManagement.Api.Extensions;
using TeamManagement.Application.Interfaces;

namespace TeamManagement.Api.Controllers
{
    //Bildirim uç noktaları listele okundu işaretle ve sayaç işlemlerini yönetir giriş zorunludur
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BildirimController : ControllerBase
    {
        //Bildirim iş mantığını yürüten servis
        private readonly IBildirimService _bildirimService;

        public BildirimController(IBildirimService bildirimService)
        {
            _bildirimService = bildirimService;
        }

        //Giriş yapan kullanıcının tüm bildirimlerini listeler en yeni en üstte
        [HttpGet("listele")]
        public async Task<IActionResult> GetBildirimler()
        {
            var kullaniciId = User.GetKullaniciId();
            if (kullaniciId == null) return Unauthorized();

            var result = await _bildirimService.GetBildirimlerAsync(kullaniciId.Value);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        //Belirli bildirimi okundu olarak işaretler başkasının bildirimi işaretlenemez
        [HttpPatch("{bildirimId}/oku")]
        public async Task<IActionResult> OkunduIsaretle(Guid bildirimId)
        {
            var kullaniciId = User.GetKullaniciId();
            if (kullaniciId == null) return Unauthorized();

            var result = await _bildirimService.OkunduIsaretle(bildirimId, kullaniciId.Value);
            if (!result.IsSuccess)
                return NotFound(result.Error);

            return Ok("Bildirim okundu olarak işaretlendi.");
        }

        //Kullanıcının tüm bildirimlerini tek seferde okundu işaretler (panel açılınca kullanılır)
        [HttpPatch("tumunu-oku")]
        public async Task<IActionResult> TumunuOkunduIsaretle()
        {
            var kullaniciId = User.GetKullaniciId();
            if (kullaniciId == null) return Unauthorized();

            var result = await _bildirimService.TumunuOkunduIsaretleAsync(kullaniciId.Value);
            return Ok(result);
        }

        //Okunmamış bildirim sayısını döndürür navbar rozetini güncellemek için kullanılır
        [HttpGet("sayac")]
        public async Task<IActionResult> GetOkunmamisSayisi()
        {
            var kullaniciId = User.GetKullaniciId();
            if (kullaniciId == null) return Unauthorized();

            var result = await _bildirimService.GetOkunmamisSayisiAsync(kullaniciId.Value);
            return result.IsSuccess ? Ok(new { OkunmamisSayi = result.Data }) : BadRequest(result);
        }
    }
}
