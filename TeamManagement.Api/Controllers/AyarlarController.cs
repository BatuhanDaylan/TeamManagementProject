using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamManagement.Api.Extensions;
using TeamManagement.Application.DTOs.Ayarlar;
using TeamManagement.Application.Interfaces;

namespace TeamManagement.Api.Controllers
{
    //Lider'in kişisel mail/AI entegrasyon ayarlarını yönetir, sadece lider erişebilir
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "EkipLideriPolicisi")]
    public class AyarlarController : ControllerBase
    {
        private readonly IMailAyarlariService _mailAyarlariService;

        public AyarlarController(IMailAyarlariService mailAyarlariService)
        {
            _mailAyarlariService = mailAyarlariService;
        }

        [HttpGet("mail")]
        public async Task<IActionResult> GetMailAyarlari()
        {
            var kullaniciId = User.GetKullaniciId();
            if (kullaniciId == null) return Unauthorized("Geçersiz kullanıcı kimliği.");

            var result = await _mailAyarlariService.GetDurumAsync(kullaniciId.Value);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPut("mail")]
        public async Task<IActionResult> KaydetMailAyarlari([FromBody] MailAyarlariDto dto)
        {
            var kullaniciId = User.GetKullaniciId();
            if (kullaniciId == null) return Unauthorized("Geçersiz kullanıcı kimliği.");

            var result = await _mailAyarlariService.KaydetAsync(kullaniciId.Value, dto);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("mail")]
        public async Task<IActionResult> KaldirMailAyarlari()
        {
            var kullaniciId = User.GetKullaniciId();
            if (kullaniciId == null) return Unauthorized("Geçersiz kullanıcı kimliği.");

            var result = await _mailAyarlariService.KaldirAsync(kullaniciId.Value);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}
