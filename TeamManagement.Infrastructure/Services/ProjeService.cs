using Microsoft.EntityFrameworkCore;
using TeamManagement.Application.DTOs.Projeler;
using TeamManagement.Application.Interfaces;
using TeamManagement.Domain.Entities;
using TeamManagement.Infrastructure.Persistence;
using TeamManagement.Application.Common.Results;

namespace TeamManagement.Infrastructure.Services
{
    //Proje yönetim servisi oluşturma listeleme ve silme işlemlerini yönetir
    public class ProjeService : IProjeService
    {
        //Proje ve görev verilerinin tutulduğu uygulama veritabanı bağlamı
        private readonly TeamManagementAppDbContext _appContext;

        public ProjeService(TeamManagementAppDbContext appContext)
        {
            _appContext = appContext;
        }

        //Aynı proje adı mailden mailden farklı yazılabiliyor (ör. "GUTAS" / "Gütas") — büyük/küçük harf
        //farkı ToLower ile zaten hallediliyor ama Türkçe aksan farkı (ü/u, ş/s, ç/c, ğ/g, ö/o, ı/i) harf
        //harf karşılaştırmada ayrı karakter sayıldığından eşleşmeyi kırıyor. Karşılaştırmadan önce her
        //iki taraf da bu şekilde sadeleştirilir ki yazım farkı proje eşleştirmesini engellemesin.
        private static string TurkceAksanSadelestir(string metin) => metin
            .Replace('ü', 'u').Replace('Ü', 'u')
            .Replace('ö', 'o').Replace('Ö', 'o')
            .Replace('ç', 'c').Replace('Ç', 'c')
            .Replace('ş', 's').Replace('Ş', 's')
            .Replace('ğ', 'g').Replace('Ğ', 'g')
            .Replace('ı', 'i').Replace('İ', 'i');

        //Yeni proje oluşturur ve belirtilen ekibe bağlar
        public async Task<Result<ProjeDto>> CreateProjeAsync(CreateProjeDto createProjeDto, Guid callerEkipId)
        {
            //DTO'daki EkipId istemciden geldiği için güvenilmez, proje her zaman çağıranın kendi ekibine bağlanır
            var yeniProje = new Proje
            {
                Id = Guid.NewGuid(),
                Ad = createProjeDto.Ad,
                Aciklama = createProjeDto.Aciklama,
                EkipId = callerEkipId,
                OluşturulmaTarihi = DateTime.UtcNow
            };

            _appContext.Projeler.Add(yeniProje);
            await _appContext.SaveChangesAsync();

            var dto = new ProjeDto
            {
                Id = yeniProje.Id,
                Ad = yeniProje.Ad,
                EkipId = yeniProje.EkipId,
                Aciklama = yeniProje.Aciklama,
                OlusturulmaTarihi = yeniProje.OluşturulmaTarihi,
                //Kurucu bilgisi şimdilik boş ileride eklenecek
                OlusturanLiderId = Guid.Empty
            };
            return Result<ProjeDto>.Success(dto);
        }

        //Belirli bir ekibe ait tüm projeleri listeler
        public async Task<Result<List<ProjeDto>>> GetProjelerByEkipIdAsync(Guid ekipId)
        {
            var projeler = await _appContext.Projeler
                .Where(p => p.EkipId == ekipId)
                .Select(p => new ProjeDto
                {
                    Id = p.Id,
                    Ad = p.Ad,
                    Aciklama = p.Aciklama,
                    EkipId = p.EkipId,
                    OlusturulmaTarihi = p.OluşturulmaTarihi
                })
                .ToListAsync();

            return Result<List<ProjeDto>>.Success(projeler);
        }

        //Proje adını/açıklamasını günceller; proje bulunamazsa veya başka ekibe aitse reddedilir
        public async Task<Result<ProjeDto>> UpdateProjeAsync(Guid projeId, UpdateProjeDto dto, Guid callerEkipId)
        {
            var proje = await _appContext.Projeler.FindAsync(projeId);
            if (proje == null) return Result<ProjeDto>.Failure("Proje bulunamadı.", 404);
            if (proje.EkipId != callerEkipId) return Result<ProjeDto>.Failure("Bu proje üzerinde yetkiniz yok.", 403);

            if (string.IsNullOrWhiteSpace(dto.Ad)) return Result<ProjeDto>.Failure("Proje adı boş olamaz.");

            proje.Ad = dto.Ad;
            proje.Aciklama = dto.Aciklama;
            await _appContext.SaveChangesAsync();

            return Result<ProjeDto>.Success(new ProjeDto
            {
                Id = proje.Id,
                Ad = proje.Ad,
                Aciklama = proje.Aciklama,
                EkipId = proje.EkipId,
                OlusturulmaTarihi = proje.OluşturulmaTarihi
            });
        }

        //Projeyi siler proje bulunamazsa veya başka ekibe aitse reddedilir
        public async Task<Result> DeleteProjeAsync(Guid projeId, Guid callerEkipId)
        {
            var proje = await _appContext.Projeler.FindAsync(projeId);
            if (proje == null) return Result.Failure("Proje bulunamadı.", 404);
            if (proje.EkipId != callerEkipId) return Result.Failure("Bu proje üzerinde yetkiniz yok.", 403);

            _appContext.Projeler.Remove(proje);
            await _appContext.SaveChangesAsync();

            return Result.Success();
        }
        public async Task<Result<ProjeDto>> GetProjeByNameAsync(string projeAdi, Guid? ekipId = null)
        {
            if (string.IsNullOrEmpty(projeAdi)) return Result<ProjeDto>.Failure("Proje adı boş olamaz.");

            var projeAdiLower = TurkceAksanSadelestir(projeAdi.Trim().ToLower());
            var sorgu = _appContext.Projeler.AsQueryable();

            //ekipId verilmişse arama o ekiple sınırlanır, farklı ekiplerdeki aynı isimli projeler karışmaz
            if (ekipId.HasValue)
                sorgu = sorgu.Where(p => p.EkipId == ekipId.Value);

            //Eşleştirme bellekte yapılır (aksan-sadeleştirme SQL tarafına çevrilemeyeceğinden);
            //adaylar zaten ekip bazında sınırlandırılmış küçük bir küme olduğu için sorun olmaz.
            var adaylar = await sorgu.ToListAsync();

            //Önce tam eşleşme denenir (AI'ın yazdığı ad sistemdekiyle birebir aynıysa en güvenilir sonuç budur)
            var proje = adaylar.FirstOrDefault(p => TurkceAksanSadelestir(p.Ad.ToLower()) == projeAdiLower);

            if (proje == null)
            {
                //Tam eşleşme bulunamazsa kısmi eşleşmeye düşülür (AI kısaltma/fazladan kelime yazmış olabilir)
                proje = adaylar.FirstOrDefault(p =>
                {
                    var adSade = TurkceAksanSadelestir(p.Ad.ToLower());
                    return adSade.Contains(projeAdiLower) || projeAdiLower.Contains(adSade);
                });
            }

            if (proje == null) return Result<ProjeDto>.Failure("Proje bulunamadı.", 404);

            var dto = new ProjeDto
            {
                Id = proje.Id,
                Ad = proje.Ad,
                Aciklama = proje.Aciklama,
                EkipId = proje.EkipId,
                OlusturulmaTarihi = proje.OluşturulmaTarihi
            };
            return Result<ProjeDto>.Success(dto);
        }
    }
}
