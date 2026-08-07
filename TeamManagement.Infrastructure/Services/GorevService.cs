using Microsoft.EntityFrameworkCore;
using TeamManagement.Application.DTOs.Gorevler;
using TeamManagement.Application.Interfaces;
using TeamManagement.Domain.Entities;
using TeamManagement.Domain.Enums;
using TeamManagement.Infrastructure.Persistence;
using TeamManagement.Application.Common.Results;

namespace TeamManagement.Infrastructure.Services
{
    //Görev iş mantığı servisi — CRUD operasyonları durum geçiş kuralları ve SignalR yayınları
    public class GorevService : IGorevService
    {
        //Uygulama veritabanı bağlamı görev proje ekip verilerini içerir
        private readonly TeamManagementAppDbContext _appContext;

        //SignalR ile anlık yayın yapabilmek için bildirim servisi inject edilir
        private readonly INotificationService _notificationService;

        //Kalıcı bildirim kayıtları (zil/sayaç) için
        private readonly IBildirimService _bildirimService;

        public GorevService(TeamManagementAppDbContext appContext, INotificationService notificationService, IBildirimService bildirimService)
        {
            _appContext = appContext;
            _notificationService = notificationService;
            _bildirimService = bildirimService;
        }

        //Bir kullanıcıya hem kalıcı (zil/sayaç) hem anlık (SignalR) bildirim ulaştırır
        //gorevId verilirse bildirime tıklandığında ilgili göreve gidilebilir
        private async Task BildirimUlastirAsync(Guid kullaniciId, string icerik, Guid? gorevId = null)
        {
            await _bildirimService.BildirimOlusturAsync(kullaniciId, icerik, gorevId);
            await _notificationService.BildirimGonderAsync(kullaniciId, icerik);
        }

        //Görevin bağlı olduğu ekibin liderinin kullanıcı ID sini bulur, ekip/proje bulunamazsa null döner
        private async Task<Guid?> GetEkipLideriIdAsync(Guid projeId)
        {
            var proje = await _appContext.Projeler.FindAsync(projeId);
            if (proje == null) return null;

            var ekip = await _appContext.Ekipler.FindAsync(proje.EkipId);
            return ekip?.OlusturanLiderId;
        }

        //PostgreSQL'deki timestamptz kolonları sadece Kind=Utc kabul eder. AI mail parser'ın JSON'dan
        //ürettiği "YYYY-MM-DD" tarihleri (saat dilimsiz) Kind=Unspecified gelir; frontend'in gönderdiği
        //toISOString() tarihleri zaten Kind=Utc olduğundan bu metot onlar için no-op'tur.
        private static DateTime? NormalizeUtc(DateTime? tarih)
        {
            if (!tarih.HasValue) return null;
            return tarih.Value.Kind == DateTimeKind.Utc
                ? tarih.Value
                : DateTime.SpecifyKind(tarih.Value, DateTimeKind.Utc);
        }

        //Yeni görev oluşturur varsayılan durum her zaman Backlog olarak başlar
        public async Task<Result<GorevDto>> CreateGorevAsync(CreateGorevDto createGorevDto, Guid callerEkipId)
        {
            //Görev başka bir ekibin projesine eklenmeye çalışılıyorsa reddedilir
            var hedefProje = await _appContext.Projeler.FindAsync(createGorevDto.ProjeId);
            if (hedefProje == null || hedefProje.EkipId != callerEkipId)
                return Result<GorevDto>.Failure("Proje bulunamadı veya bu ekibe ait değil.", 403);

            var yeniGorev = new Gorev
            {
                Id = Guid.NewGuid(),
                Baslik = createGorevDto.Baslik,
                Aciklama = createGorevDto.Aciklama,
                Oncelik = createGorevDto.Oncelik,
                //Baslangic ve bitis tarihleri opsiyonel haftalik mail veya manuel girilebilir
                BaslangicTarihi = NormalizeUtc(createGorevDto.BaslangicTarihi),
                Deadline = NormalizeUtc(createGorevDto.Deadline),
                ProjeId = createGorevDto.ProjeId,
                AtananId = createGorevDto.AtananId,
                Durum = GorevDurumu.Backlog,
                OluşturulmaTarihi = DateTime.UtcNow
            };

            _appContext.Gorevler.Add(yeniGorev);
            await _appContext.SaveChangesAsync();

            //Oluşturulan görevi DTO formatında döndür
            var dto = new GorevDto
            {
                Id = yeniGorev.Id,
                Baslik = yeniGorev.Baslik,
                Aciklama = yeniGorev.Aciklama,
                Durum = yeniGorev.Durum,
                Oncelik = yeniGorev.Oncelik,
                BaslangicTarihi = yeniGorev.BaslangicTarihi,
                Deadline = yeniGorev.Deadline,
                ProjeId = yeniGorev.ProjeId,
                AtananId = yeniGorev.AtananId,
                OluşturulmaTarihi = yeniGorev.OluşturulmaTarihi
            };

            // Yeni görev oluşturulduğunda da SignalR ile tüm istemcilere bildir
            await _notificationService.GorevGuncellendiAsync(yeniGorev.ProjeId, dto);

            //Görev oluşturulurken bir kullanıcıya atanmışsa o kullanıcıya bildirim gönderilir
            if (yeniGorev.AtananId.HasValue)
                await BildirimUlastirAsync(yeniGorev.AtananId.Value, $"'{yeniGorev.Baslik}' görevi size atandı.", yeniGorev.Id);

            return Result<GorevDto>.Success(dto);
        }

        //Belirli bir projeye ait tüm görevleri listeler kanban panosu için kullanılır
        public async Task<Result<List<GorevDto>>> GetGorevlerByProjeIdAsync(Guid projeId, Guid callerEkipId)
        {
            var proje = await _appContext.Projeler.FindAsync(projeId);
            if (proje == null || proje.EkipId != callerEkipId)
                return Result<List<GorevDto>>.Failure("Proje bulunamadı veya bu ekibe ait değil.", 403);

            var gorevler = await _appContext.Gorevler
                .Where(g => g.ProjeId == projeId)
                .Select(g => new GorevDto
                {
                    Id = g.Id,
                    Baslik = g.Baslik,
                    Aciklama = g.Aciklama,
                    Durum = g.Durum,
                    Oncelik = g.Oncelik,
                    BaslangicTarihi = g.BaslangicTarihi,
                    Deadline = g.Deadline,
                    ProjeId = g.ProjeId,
                    AtananId = g.AtananId,
                    OluşturulmaTarihi = g.OluşturulmaTarihi
                })
                .ToListAsync();

            return Result<List<GorevDto>>.Success(gorevler);
        }

        //Basit durum güncelleme e-posta aksiyon linkleri ve AI parser tarafından kullanılır
        public async Task<Result> UpdateGorevDurumAsync(Guid gorevId, UpdateGorevDurumDto updateDto)
        {
            var gorev = await _appContext.Gorevler.FindAsync(gorevId);
            if (gorev == null) return Result.Failure("Görev bulunamadı.", 404);

            gorev.Durum = updateDto.YeniDurum;
            await _appContext.SaveChangesAsync();
            return Result.Success();
        }

        //Başlık içeriğine göre görev arar, durumunu günceller ve opsiyonel olarak AI yorumunu (ilerleme notunu) ekler
        public async Task<Result> UpdateGorevDurumByBaslikAsync(string baslik, GorevDurumu yeniDurum, string? ilerlemeNotu = null, Guid? yazanId = null, Guid? ekipId = null, string? gondericiRol = null, string? kaynakEmailMetni = null)
        {
            if (string.IsNullOrEmpty(baslik)) return Result.Failure("Başlık boş olamaz.");

            //Kanban panosundaki kuralla aynı: Lider olmayan (veya sistemde tanınmayan) bir gönderen mail ile
            //görevi doğrudan Tamamlandı yapamaz, nihai onay her zaman ekip liderinden gelmelidir
            if (yeniDurum == GorevDurumu.Tamamlandi && gondericiRol != nameof(TeamManagement.Domain.Enums.Rol.Lider))
                yeniDurum = GorevDurumu.OnayBekliyor;

            //Görev başlığı büyük küçük harf duyarsız aranır. ToLowerInvariant kullanılmalı: sunucunun
            //thread kültürü tr-TR olduğunda CLR'nin kültüre duyarlı ToLower()'ı büyük "I" harfini
            //noktasız "ı"ya çeviriyor (Türkçe I problemi), ama bu Where ifadesindeki "baslik" bir yerel
            //değişken olduğundan EF Core onu istemci tarafında değerlendirip parametre olarak gönderiyor;
            //buna karşılık "g.Baslik.ToLower()" SQL'e çevrilip PostgreSQL'in İngilizce locale'ıyla (en_US.utf8)
            //normal "i"ye çevriliyor. Sonuç: "API" gibi büyük I içeren başlıklar hiçbir zaman eşleşmiyordu.
            var sorgu = _appContext.Gorevler.Where(g => g.Baslik.ToLower().Contains(baslik.ToLowerInvariant()));

            //ekipId verilmişse arama o ekibin projeleriyle sınırlanır, aksi halde tüm ekipler taranır
            //(eski davranışla geriye dönük uyum için ekipId opsiyoneldir)
            if (ekipId.HasValue)
            {
                var projeIdler = await _appContext.Projeler
                    .Where(p => p.EkipId == ekipId.Value)
                    .Select(p => p.Id)
                    .ToListAsync();
                sorgu = sorgu.Where(g => projeIdler.Contains(g.ProjeId));
            }

            var gorev = await sorgu.FirstOrDefaultAsync();
            if (gorev == null) return Result.Failure("Görev bulunamadı.", 404);

            //Lider disindaki bir gonderen sadece kendisine atanmis gorevi guncelleyebilir — Kanban
            //panosundaki UpdateGorevDurumWithRoleCheckAsync'teki aynı sahiplik kuralı mail akışında da geçerli
            if (gondericiRol != nameof(TeamManagement.Domain.Enums.Rol.Lider) && gorev.AtananId != yazanId)
                return Result.Failure("Bu görev size atanmamış, güncelleyemezsiniz.", 403);

            var mevcutDurum = gorev.Durum;
            gorev.Durum = yeniDurum;

            // Eğer bir AI ilerleme notu gönderilmişse "İlerlemeler (AI)" sekmesinde
            // görünmesi için kaynak mail metniyle birlikte göreve iliştirilir
            if (!string.IsNullOrWhiteSpace(ilerlemeNotu))
            {
                var ilerleme = new GorevIlerleme
                {
                    GorevId = gorev.Id,
                    TamamlananKisim = ilerlemeNotu,
                    KaynakEmailMetni = kaynakEmailMetni ?? string.Empty,
                    EkleyenKullaniciId = yazanId,
                    OluşturulmaTarihi = DateTime.UtcNow
                };
                _appContext.GorevIlerlemeleri.Add(ilerleme);
            }

            await _appContext.SaveChangesAsync();

            // Yorum (veya sadece durum) eklendikten sonra anlık (SignalR) olarak frontend'leri haberdar et
            var dto = new GorevDto
            {
                Id = gorev.Id,
                Baslik = gorev.Baslik,
                Aciklama = gorev.Aciklama,
                Durum = gorev.Durum,
                Oncelik = gorev.Oncelik,
                BaslangicTarihi = gorev.BaslangicTarihi,
                Deadline = gorev.Deadline,
                ProjeId = gorev.ProjeId,
                AtananId = gorev.AtananId,
                OluşturulmaTarihi = gorev.OluşturulmaTarihi
            };
            await _notificationService.GorevGuncellendiAsync(gorev.ProjeId, dto);

            //Durum gerçekten değiştiyse ilgili kişilere bildirim gönderilir (Kanban akışıyla aynı kural)
            if (mevcutDurum != yeniDurum)
            {
                if (yeniDurum == GorevDurumu.OnayBekliyor)
                {
                    var liderId = await GetEkipLideriIdAsync(gorev.ProjeId);
                    if (liderId.HasValue)
                        await BildirimUlastirAsync(liderId.Value, $"'{gorev.Baslik}' görevi onayınızı bekliyor.", gorev.Id);
                }
                else if (yeniDurum == GorevDurumu.Tamamlandi && gondericiRol == nameof(TeamManagement.Domain.Enums.Rol.Lider) && gorev.AtananId.HasValue)
                {
                    await BildirimUlastirAsync(gorev.AtananId.Value, $"'{gorev.Baslik}' göreviniz tamamlandı olarak onaylandı.", gorev.Id);
                }
            }

            return Result.Success();
        }

        //Görevi tamamen siler sadece lider yapabilir controller katmanında yetki kontrolü yapılır
        public async Task<Result> DeleteGorevAsync(Guid gorevId, Guid callerEkipId)
        {
            var gorev = await _appContext.Gorevler.FindAsync(gorevId);
            if (gorev == null) return Result.Failure("Görev bulunamadı.", 404);

            //Görevin bağlı olduğu proje, isteği yapanın ekibine ait değilse reddedilir
            var proje = await _appContext.Projeler.FindAsync(gorev.ProjeId);
            if (proje == null || proje.EkipId != callerEkipId)
                return Result.Failure("Bu görev üzerinde yetkiniz yok.", 403);

            _appContext.Gorevler.Remove(gorev);
            await _appContext.SaveChangesAsync();
            return Result.Success();
        }

        //Partial update sadece null olmayan alanlar güncellenir gönderilmeyen alanlar korunur
        public async Task<Result<GorevDto>> UpdateGorevAsync(Guid gorevId, UpdateGorevDto updateDto, Guid callerEkipId)
        {
            var gorev = await _appContext.Gorevler.FindAsync(gorevId);
            if (gorev == null) return Result<GorevDto>.Failure("Görev bulunamadı.", 404);

            //Görevin bağlı olduğu proje, isteği yapanın ekibine ait değilse reddedilir
            var sahipProje = await _appContext.Projeler.FindAsync(gorev.ProjeId);
            if (sahipProje == null || sahipProje.EkipId != callerEkipId)
                return Result<GorevDto>.Failure("Bu görev üzerinde yetkiniz yok.", 403);

            //Atama değişikliği bildirim gönderebilmek için güncelleme öncesi karşılaştırılır
            var oncekiAtananId = gorev.AtananId;

            //Her alan için null kontrolü yapılır sadece gönderilen alanlar üzerine yazılır
            if (updateDto.Baslik != null) gorev.Baslik = updateDto.Baslik;
            if (updateDto.Aciklama != null) gorev.Aciklama = updateDto.Aciklama;
            if (updateDto.Oncelik != null) gorev.Oncelik = updateDto.Oncelik;
            if (updateDto.BaslangicTarihi.HasValue) gorev.BaslangicTarihi = NormalizeUtc(updateDto.BaslangicTarihi);
            if (updateDto.Deadline.HasValue) gorev.Deadline = NormalizeUtc(updateDto.Deadline);
            if (updateDto.AtananId.HasValue) gorev.AtananId = updateDto.AtananId;

            await _appContext.SaveChangesAsync();

            //Görev yeni/farklı bir kullanıcıya atandıysa o kullanıcıya bildirim gönderilir
            if (updateDto.AtananId.HasValue && updateDto.AtananId != oncekiAtananId)
                await BildirimUlastirAsync(updateDto.AtananId.Value, $"'{gorev.Baslik}' görevi size atandı.", gorev.Id);

            var dto = new GorevDto
            {
                Id = gorev.Id,
                Baslik = gorev.Baslik,
                Aciklama = gorev.Aciklama,
                Durum = gorev.Durum,
                Oncelik = gorev.Oncelik,
                BaslangicTarihi = gorev.BaslangicTarihi,
                Deadline = gorev.Deadline,
                ProjeId = gorev.ProjeId,
                AtananId = gorev.AtananId,
                OluşturulmaTarihi = gorev.OluşturulmaTarihi
            };
            return Result<GorevDto>.Success(dto);
        }

        //Ekibe ait tum projelerdeki gorevleri getirir dashboard icin kullanilir
        public async Task<Result<List<GorevDto>>> GetGorevlerByEkipIdAsync(Guid ekipId)
        {
            //Once ekibin projelerini bul sonra o projelerin gorevlerini getir
            var projeIdler = await _appContext.Projeler
                .Where(p => p.EkipId == ekipId)
                .Select(p => p.Id)
                .ToListAsync();

            var gorevler = await _appContext.Gorevler
                .Where(g => projeIdler.Contains(g.ProjeId))
                .Select(g => new GorevDto
                {
                    Id = g.Id,
                    Baslik = g.Baslik,
                    Aciklama = g.Aciklama,
                    Durum = g.Durum,
                    Oncelik = g.Oncelik,
                    BaslangicTarihi = g.BaslangicTarihi,
                    Deadline = g.Deadline,
                    ProjeId = g.ProjeId,
                    AtananId = g.AtananId,
                    OluşturulmaTarihi = g.OluşturulmaTarihi
                })
                .ToListAsync();
            return Result<List<GorevDto>>.Success(gorevler);
        }

        //Gorev basligina ve proje ID sine gore gorev bulur Iptal durumuna alir
        //Haftalik mail parser CIKAR bolumundeki gorevler icin kullanilir
        public async Task<Result> GorevIptalEtByBaslikAsync(string baslik, Guid projeId, Guid? yazanId = null, string? gondericiRol = null)
        {
            if (string.IsNullOrEmpty(baslik)) return Result.Failure("Başlık boş olamaz.");

            //Baslik iceren gorevi proje kapsaminda ara buyuk kucuk harf duyarsiz (bkz. UpdateGorevDurumByBaslikAsync'teki
            //Türkçe I problemi notu — ToLowerInvariant, sunucu kültürü tr-TR olsa da PostgreSQL'in ürettiği "i" ile tutarlı kalır)
            var gorev = await _appContext.Gorevler
                .FirstOrDefaultAsync(g =>
                    g.ProjeId == projeId &&
                    g.Baslik.ToLower().Contains(baslik.ToLowerInvariant()));

            if (gorev == null) return Result.Failure("Görev bulunamadı.", 404);

            //Lider disindaki bir gonderen sadece kendisine atanmis gorevi iptal edebilir
            if (gondericiRol != nameof(TeamManagement.Domain.Enums.Rol.Lider) && gorev.AtananId != yazanId)
                return Result.Failure("Bu görev size atanmamış, iptal edemezsiniz.", 403);

            //Tamamen silme yerine Iptal olarak isaretlenir gecmis kayit korunur
            gorev.Durum = GorevDurumu.Iptal;
            await _appContext.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<Result> GorevAlanlariniGuncelleByBaslikAsync(string baslik, Guid projeId, string? yeniOncelik, DateTime? yeniBaslangicTarihi, DateTime? yeniBitisTarihi, Guid? yazanId = null, string? gondericiRol = null)
        {
            if (string.IsNullOrEmpty(baslik)) return Result.Failure("Başlık boş olamaz.");

            //Baslik iceren gorevi proje kapsaminda ara buyuk kucuk harf duyarsiz (GorevIptalEtByBaslikAsync ile ayni yontem,
            //ToLowerInvariant nedeni icin UpdateGorevDurumByBaslikAsync'teki Türkçe I problemi notuna bakın)
            var gorev = await _appContext.Gorevler
                .FirstOrDefaultAsync(g =>
                    g.ProjeId == projeId &&
                    g.Baslik.ToLower().Contains(baslik.ToLowerInvariant()));

            if (gorev == null) return Result.Failure("Görev bulunamadı.", 404);

            //Lider disindaki bir gonderen sadece kendisine atanmis gorevi duzenleyebilir
            if (gondericiRol != nameof(TeamManagement.Domain.Enums.Rol.Lider) && gorev.AtananId != yazanId)
                return Result.Failure("Bu görev size atanmamış, düzenleyemezsiniz.", 403);

            //Sadece mailde acikca belirtilen alanlar degistirilir, digerleri oldugu gibi kalir
            //(NormalizeUtc: Npgsql Kind=Unspecified DateTime'i timestamptz kolonuna yazmayi reddediyor)
            if (!string.IsNullOrEmpty(yeniOncelik)) gorev.Oncelik = yeniOncelik;
            if (yeniBaslangicTarihi.HasValue) gorev.BaslangicTarihi = NormalizeUtc(yeniBaslangicTarihi);
            if (yeniBitisTarihi.HasValue) gorev.Deadline = NormalizeUtc(yeniBitisTarihi);

            await _appContext.SaveChangesAsync();
            return Result.Success();
        }

        //Rol bazlı durum geçiş kontrolü ile güncelleme yapar ve başarılı olunca SignalR ile yayın yapar
        public async Task<Result> UpdateGorevDurumWithRoleCheckAsync(
            Guid gorevId,
            UpdateGorevDurumDto updateDto,
            Guid kullaniciId,
            string kullaniciRol,
            Guid callerEkipId)
        {
            var gorev = await _appContext.Gorevler.FindAsync(gorevId);
            if (gorev == null) return Result.Failure("Görev bulunamadı.", 404);

            //Görevin bağlı olduğu proje, isteği yapanın ekibine ait değilse reddedilir
            var sahipProje = await _appContext.Projeler.FindAsync(gorev.ProjeId);
            if (sahipProje == null || sahipProje.EkipId != callerEkipId)
                return Result.Failure("Bu görev üzerinde yetkiniz yok.", 403);

            var mevcutDurum = gorev.Durum;
            var yeniDurum = updateDto.YeniDurum;

            if (kullaniciRol == "Lider")
            {
                //Lider her geçişi yapabilir zorla taşıma ve Tamamlandi onayı dahil
                gorev.Durum = yeniDurum;
            }
            else
            {
                //Üye sadece kendine atanan görevi güncelleyebilir
                if (gorev.AtananId != kullaniciId)
                    return Result.Failure("Sadece size atanan görevleri güncelleyebilirsiniz.", 403);

                //Üye için izin verilen geçiş haritası sadece ileri yönlü OnayBekliyor a kadar
                var gecerliGecisler = new Dictionary<GorevDurumu, GorevDurumu>
                {
                    { GorevDurumu.Backlog,     GorevDurumu.DevamEdiyor },
                    { GorevDurumu.DevamEdiyor, GorevDurumu.Test },
                    { GorevDurumu.Test,        GorevDurumu.OnayBekliyor },
                };

                //İzin verilmeyen geçiş girişimi reddedilir
                if (!gecerliGecisler.TryGetValue(mevcutDurum, out var izinVerilenDurum) ||
                    izinVerilenDurum != yeniDurum)
                {
                    return Result.Failure("Rolünüz bu durum geçişi için yetkisiz.", 403);
                }

                gorev.Durum = yeniDurum;
            }

            await _appContext.SaveChangesAsync();

            //Durum güncelleme başarılıysa aynı projedeki tüm bağlı istemcilere anlık yayın yapılır
            var gorevDto = new GorevDto
            {
                Id = gorev.Id,
                Baslik = gorev.Baslik,
                Aciklama = gorev.Aciklama,
                Durum = gorev.Durum,
                Oncelik = gorev.Oncelik,
                BaslangicTarihi = gorev.BaslangicTarihi,
                Deadline = gorev.Deadline,
                ProjeId = gorev.ProjeId,
                AtananId = gorev.AtananId,
                OluşturulmaTarihi = gorev.OluşturulmaTarihi
            };

            await _notificationService.GorevGuncellendiAsync(gorev.ProjeId, gorevDto);

            //Durum gerçekten değiştiyse ilgili kişilere bildirim gönderilir
            if (mevcutDurum != yeniDurum)
            {
                if (yeniDurum == GorevDurumu.OnayBekliyor)
                {
                    //Üye görevi onaya gönderdiğinde ekip liderine haber verilir
                    var liderId = await GetEkipLideriIdAsync(gorev.ProjeId);
                    if (liderId.HasValue)
                        await BildirimUlastirAsync(liderId.Value, $"'{gorev.Baslik}' görevi onayınızı bekliyor.", gorev.Id);
                }
                else if (yeniDurum == GorevDurumu.Tamamlandi && kullaniciRol == "Lider" && gorev.AtananId.HasValue)
                {
                    //Lider görevi onaylayıp tamamlandı yaptığında atanan kişiye haber verilir
                    await BildirimUlastirAsync(gorev.AtananId.Value, $"'{gorev.Baslik}' göreviniz tamamlandı olarak onaylandı.", gorev.Id);
                }
            }

            return Result.Success();
        }

        //Sahipsiz (AtananId'i boş) bir görevi çağıran kullanıcıya atar. Mail akışından oluşan görevlerin
        //hiçbir üyeye atanmaması nedeniyle Kanban'da ayrı bir "Sahipsiz" grupta gösterilirler; bir üye
        //bu görevi Backlog'a sürükleyerek üstlenir. Zaten atanmış bir görev tekrar üstlenilemez ve bu
        //işlem geri alınamaz (görev "sahipsiz" haline döndürülemez, sadece Lider yeniden atayabilir).
        public async Task<Result> GorevUstlenAsync(Guid gorevId, Guid kullaniciId, Guid callerEkipId)
        {
            var gorev = await _appContext.Gorevler.FindAsync(gorevId);
            if (gorev == null) return Result.Failure("Görev bulunamadı.", 404);

            var sahipProje = await _appContext.Projeler.FindAsync(gorev.ProjeId);
            if (sahipProje == null || sahipProje.EkipId != callerEkipId)
                return Result.Failure("Bu görev üzerinde yetkiniz yok.", 403);

            if (gorev.AtananId != null)
                return Result.Failure("Bu görev zaten bir kullanıcıya atanmış.", 400);

            gorev.AtananId = kullaniciId;
            await _appContext.SaveChangesAsync();

            var gorevDto = new GorevDto
            {
                Id = gorev.Id,
                Baslik = gorev.Baslik,
                Aciklama = gorev.Aciklama,
                Durum = gorev.Durum,
                Oncelik = gorev.Oncelik,
                BaslangicTarihi = gorev.BaslangicTarihi,
                Deadline = gorev.Deadline,
                ProjeId = gorev.ProjeId,
                AtananId = gorev.AtananId,
                OluşturulmaTarihi = gorev.OluşturulmaTarihi
            };

            await _notificationService.GorevGuncellendiAsync(gorev.ProjeId, gorevDto);

            return Result.Success();
        }
    }
}
