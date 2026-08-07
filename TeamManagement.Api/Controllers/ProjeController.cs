using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamManagement.Api.Extensions;
using TeamManagement.Application.DTOs.Projeler;
using TeamManagement.Application.Interfaces;

namespace TeamManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Sadece giriş yapan kullanıcılar erişebilir
    public class ProjeController : ControllerBase
    {
        private readonly IProjeService _projeService;

        public ProjeController(IProjeService projeService)
        {
            _projeService = projeService;
        }

        [HttpPost("olustur")]
        public async Task<IActionResult> CreateProje([FromBody] CreateProjeDto createProjeDto)
        {
            var callerEkipId = User.GetEkipId();
            if (callerEkipId == null) return Unauthorized("Geçersiz kullanıcı kimliği.");

            var result = await _projeService.CreateProjeAsync(createProjeDto, callerEkipId.Value);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("ekip/{ekipId}")]
        public async Task<IActionResult> GetProjelerByEkipId(Guid ekipId)
        {
            var callerEkipId = User.GetEkipId();
            if (callerEkipId == null || callerEkipId != ekipId)
                return Forbid();

            var result = await _projeService.GetProjelerByEkipIdAsync(ekipId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{projeId}")]
        public async Task<IActionResult> UpdateProje(Guid projeId, [FromBody] UpdateProjeDto dto)
        {
            var callerEkipId = User.GetEkipId();
            if (callerEkipId == null) return Unauthorized("Geçersiz kullanıcı kimliği.");

            var result = await _projeService.UpdateProjeAsync(projeId, dto, callerEkipId.Value);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("sil/{projeId}")]
        public async Task<IActionResult> DeleteProje(Guid projeId)
        {
            var callerEkipId = User.GetEkipId();
            if (callerEkipId == null) return Unauthorized("Geçersiz kullanıcı kimliği.");

            var result = await _projeService.DeleteProjeAsync(projeId, callerEkipId.Value);
            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok("Proje başarıyla silindi.");
        }
    }
}
