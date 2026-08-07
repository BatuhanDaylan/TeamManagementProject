using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamManagement.Api.Extensions;
using TeamManagement.Application.Interfaces;

namespace TeamManagement.Api.Controllers
{
    //Liderin talep üzerine kişi bazlı çalışma özeti + hata analizi maili gönderebilmesini sağlar
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "EkipLideriPolicisi")]
    public class OzetController : ControllerBase
    {
        private readonly IEkipOzetiService _ekipOzetiService;

        public OzetController(IEkipOzetiService ekipOzetiService)
        {
            _ekipOzetiService = ekipOzetiService;
        }

        [HttpPost("gonder")]
        public async Task<IActionResult> Gonder()
        {
            var ekipId = User.GetEkipId();
            if (ekipId == null) return Unauthorized("Geçersiz ekip kimliği.");

            var result = await _ekipOzetiService.HaftalikOzetGonderAsync(ekipId.Value);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        //Daha önce üretilmiş özetlerin arşivini listeler
        [HttpGet("gecmis")]
        public async Task<IActionResult> GetGecmis()
        {
            var ekipId = User.GetEkipId();
            if (ekipId == null) return Unauthorized("Geçersiz ekip kimliği.");

            var result = await _ekipOzetiService.GetGecmisOzetlerAsync(ekipId.Value);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        //Arşivdeki tek bir özeti siler
        [HttpDelete("gecmis/{ozetId}")]
        public async Task<IActionResult> DeleteGecmis(Guid ozetId)
        {
            var ekipId = User.GetEkipId();
            if (ekipId == null) return Unauthorized("Geçersiz ekip kimliği.");

            var result = await _ekipOzetiService.OzetSilAsync(ozetId, ekipId.Value);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}
