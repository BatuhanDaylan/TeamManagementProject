using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamManagement.Api.Extensions;
using TeamManagement.Application.Interfaces;

namespace TeamManagement.Api.Controllers
{
    //GorevIlerleme uç noktaları — AI mail parser'ın göreve eklediği ilerleme kayıtlarını listeler
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GorevIlerlemeController : ControllerBase
    {
        private readonly IGorevIlerlemeService _gorevIlerlemeService;

        public GorevIlerlemeController(IGorevIlerlemeService gorevIlerlemeService)
        {
            _gorevIlerlemeService = gorevIlerlemeService;
        }

        //Göreve ait AI ilerleme kayıtlarını getirir hem lider hem üye erişebilir
        [HttpGet("gorev/{gorevId}")]
        public async Task<IActionResult> GetIlerlemeler(Guid gorevId)
        {
            var callerEkipId = User.GetEkipId();
            if (callerEkipId == null) return Unauthorized("Geçersiz kullanıcı kimliği.");

            var result = await _gorevIlerlemeService.GetIlerlemelerByGorevIdAsync(gorevId, callerEkipId.Value);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}
