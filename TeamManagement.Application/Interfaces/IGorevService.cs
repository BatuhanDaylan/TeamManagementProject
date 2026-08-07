using TeamManagement.Application.DTOs.Gorevler;
using TeamManagement.Domain.Enums;
using TeamManagement.Application.Common.Results;

namespace TeamManagement.Application.Interfaces
{
    public interface IGorevService
    {
        //callerEkipId: istegi yapan kullanicinin token'daki ekip kimligi, kaynak baska ekibe aitse islem reddedilir
        Task<Result<GorevDto>> CreateGorevAsync(CreateGorevDto createGorevDto, Guid callerEkipId);
        Task<Result<List<GorevDto>>> GetGorevlerByProjeIdAsync(Guid projeId, Guid callerEkipId);
        //Ekibe ait tum gorevleri getirir dashboard icin kullanilir
        Task<Result<List<GorevDto>>> GetGorevlerByEkipIdAsync(Guid ekipId);
        Task<Result> UpdateGorevDurumAsync(Guid gorevId, UpdateGorevDurumDto updateDto);
        //Başlık içeriğine göre görev arar, durumunu günceller ve opsiyonel yorum ekler (AI e-posta parser için kullanılır)
        //ekipId verilirse arama sadece o ekibin projelerindeki görevlerle sınırlanır (farklı ekiplerde aynı/benzer
        //başlıklı görevler varsa yanlış ekibin görevinin güncellenmesini önler)
        //gondericiRol "Lider" değilse (veya bilinmiyorsa) Tamamlandı isteği otomatik olarak OnayBekliyor'a düşürülür —
        //kanban panosundaki aynı kural (üye nihai onayı veremez) mail akışında da uygulanmış olur
        //kaynakEmailMetni verilirse ilerlemeNotu ile birlikte "İlerlemeler (AI)" sekmesine referans için kaydedilir
        Task<Result> UpdateGorevDurumByBaslikAsync(string baslik, GorevDurumu yeniDurum, string? ilerlemeNotu = null, Guid? yazanId = null, Guid? ekipId = null, string? gondericiRol = null, string? kaynakEmailMetni = null);
        Task<Result> DeleteGorevAsync(Guid gorevId, Guid callerEkipId);
        Task<Result<GorevDto>> UpdateGorevAsync(Guid gorevId, UpdateGorevDto updateDto, Guid callerEkipId);
        Task<Result> UpdateGorevDurumWithRoleCheckAsync(Guid gorevId, UpdateGorevDurumDto updateDto, Guid kullaniciId, string kullaniciRol, Guid callerEkipId);
        //Haftalik mail parser tarafindan cikari lan gorevleri Iptal durumuna alir
        //Lider disindaki bir gonderen sadece KENDISINE atanmis gorevi iptal edebilir (Kanban'daki
        //"sadece size atanan gorevi guncelleyebilirsiniz" kuraliyla ayni, mail akisinda da uygulanir)
        Task<Result> GorevIptalEtByBaslikAsync(string baslik, Guid projeId, Guid? yazanId = null, string? gondericiRol = null);
        //Haftalik mail parser tarafindan cikarilan, var olan bir gorevin oncelik/tarih alanlarini
        //gunceller (durum degil); null birakilan alanlar degistirilmez. Sahiplik kurali yukaridakiyle ayni.
        Task<Result> GorevAlanlariniGuncelleByBaslikAsync(string baslik, Guid projeId, string? yeniOncelik, DateTime? yeniBaslangicTarihi, DateTime? yeniBitisTarihi, Guid? yazanId = null, string? gondericiRol = null);
        //Sahipsiz (AtananId'i bos) bir gorevi cagiran kullaniciya atar, geri alinamaz
        Task<Result> GorevUstlenAsync(Guid gorevId, Guid kullaniciId, Guid callerEkipId);
    }
}
