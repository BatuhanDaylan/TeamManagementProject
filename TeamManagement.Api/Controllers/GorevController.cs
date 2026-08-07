using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamManagement.Api.Extensions;
using TeamManagement.Application.DTOs.Gorevler;
using TeamManagement.Application.Interfaces;

namespace TeamManagement.Api.Controllers
{
    //Görev uç noktaları oluşturma listeleme durum güncelleme ve silme işlemlerini yönetir
    [ApiController]
    [Route("api/[controller]")]
    //Tüm uç noktalar giriş yapılmasını zorunlu kılar
    [Authorize]
    public class GorevController : ControllerBase
    {
        //Görev iş mantığını yürüten servis
        private readonly IGorevService _gorevService;

        public GorevController(IGorevService gorevService)
        {
            _gorevService = gorevService;
        }

        //Yeni görev oluşturma sadece lider yapabilir varsayılan durum Backlog olur
        [HttpPost("olustur")]
        [Authorize(Policy = "EkipLideriPolicisi")]
        public async Task<IActionResult> CreateGorev([FromBody] CreateGorevDto createGorevDto)
        {
            var callerEkipId = User.GetEkipId();
            if (callerEkipId == null) return Unauthorized("Geçersiz kullanıcı kimliği.");

            var result = await _gorevService.CreateGorevAsync(createGorevDto, callerEkipId.Value);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        //Ekibin tüm görevlerini listeler (atanmış/atanmamış fark etmeksizin) kanban panosu için kullanılır.
        //Dashboard/ekip/{id} endpoint'i sadece üyelere atanmış görevleri döndürdüğünden (atanmamış
        //görevler mail akışından oluştuğunda hiçbir üyeye bağlı olmadığı için orada görünmez), Kanban
        //panosu görev listesini bu uçtan alır.
        [HttpGet("ekip/{ekipId}")]
        public async Task<IActionResult> GetGorevlerByEkipId(Guid ekipId)
        {
            var callerEkipId = User.GetEkipId();
            if (callerEkipId == null || callerEkipId != ekipId)
                return Forbid();

            var result = await _gorevService.GetGorevlerByEkipIdAsync(ekipId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        //Projeye ait görevleri listeler kanban panosu için kullanılır
        [HttpGet("proje/{projeId}")]
        public async Task<IActionResult> GetGorevlerByProjeId(Guid projeId)
        {
            var callerEkipId = User.GetEkipId();
            if (callerEkipId == null) return Unauthorized("Geçersiz kullanıcı kimliği.");

            var result = await _gorevService.GetGorevlerByProjeIdAsync(projeId, callerEkipId.Value);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        //Rol bazlı durum güncelleme token dan kullanıcı ID ve rol okunur iş kuralları serviste uygulanır
        [HttpPatch("{gorevId}/durum")]
        public async Task<IActionResult> UpdateDurum(Guid gorevId, [FromBody] UpdateGorevDurumDto updateDto)
        {
            var kullaniciId = User.GetKullaniciId();
            if (kullaniciId == null) return Unauthorized("Geçersiz kullanıcı kimliği.");

            var callerEkipId = User.GetEkipId();
            if (callerEkipId == null) return Unauthorized("Geçersiz kullanıcı kimliği.");

            //Rol token dan alınır yoksa Uye varsayılır
            var kullaniciRol = User.GetRol();

            var result = await _gorevService.UpdateGorevDurumWithRoleCheckAsync(
                gorevId, updateDto, kullaniciId.Value, kullaniciRol, callerEkipId.Value);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok("Görev durumu başarıyla güncellendi.");
        }

        //Sahipsiz bir görevi çağıran kullanıcıya atar (Kanban'da "Sahipsiz" gruptan Backlog'a taşırken
        //kullanılır). Herhangi bir ekip üyesi kullanabilir, zaten atanmış bir görev tekrar üstlenilemez.
        [HttpPatch("{gorevId}/ustlen")]
        public async Task<IActionResult> GorevUstlen(Guid gorevId)
        {
            var kullaniciId = User.GetKullaniciId();
            if (kullaniciId == null) return Unauthorized("Geçersiz kullanıcı kimliği.");

            var callerEkipId = User.GetEkipId();
            if (callerEkipId == null) return Unauthorized("Geçersiz kullanıcı kimliği.");

            var result = await _gorevService.GorevUstlenAsync(gorevId, kullaniciId.Value, callerEkipId.Value);
            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        //Görev detaylarını günceller sadece lider yapabilir null alanlar değiştirilmez
        [HttpPut("{gorevId}")]
        [Authorize(Policy = "EkipLideriPolicisi")]
        public async Task<IActionResult> UpdateGorev(Guid gorevId, [FromBody] UpdateGorevDto updateDto)
        {
            var callerEkipId = User.GetEkipId();
            if (callerEkipId == null) return Unauthorized("Geçersiz kullanıcı kimliği.");

            var result = await _gorevService.UpdateGorevAsync(gorevId, updateDto, callerEkipId.Value);
            if (!result.IsSuccess)
                return NotFound(result.Error);

            return Ok(result);
        }

        //Görevi tamamen siler sadece lider yapabilir
        [HttpDelete("sil/{gorevId}")]
        [Authorize(Policy = "EkipLideriPolicisi")]
        public async Task<IActionResult> DeleteGorev(Guid gorevId)
        {
            var callerEkipId = User.GetEkipId();
            if (callerEkipId == null) return Unauthorized("Geçersiz kullanıcı kimliği.");

            var result = await _gorevService.DeleteGorevAsync(gorevId, callerEkipId.Value);
            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok("Görev başarıyla silindi.");
        }
    }
}
