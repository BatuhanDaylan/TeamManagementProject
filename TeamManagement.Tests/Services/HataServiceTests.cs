using Microsoft.EntityFrameworkCore;
using TeamManagement.Application.DTOs.Hata;
using TeamManagement.Domain.Entities;
using TeamManagement.Domain.Enums;
using TeamManagement.Infrastructure.Persistence;
using TeamManagement.Infrastructure.Services;

namespace TeamManagement.Tests.Services
{
    public class HataServiceTests
    {
        private static TeamManagementAppDbContext YeniContext()
        {
            var options = new DbContextOptionsBuilder<TeamManagementAppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new TeamManagementAppDbContext(options);
        }

        //Bir hatanın bağlı olabileceği ekip -> proje -> görev zincirini InMemory db'de hazırlar
        private static async Task<(Guid EkipId, Guid GorevId)> OrnekVeriOlusturAsync(TeamManagementAppDbContext context)
        {
            var ekip = new Ekip { Id = Guid.NewGuid(), Ad = "Test Ekibi", OlusturanLiderId = Guid.NewGuid() };
            var proje = new Proje { Id = Guid.NewGuid(), Ad = "Test Projesi", EkipId = ekip.Id };
            var gorev = new Gorev
            {
                Id = Guid.NewGuid(),
                Baslik = "Test Görevi",
                Aciklama = "",
                Durum = GorevDurumu.Backlog,
                Oncelik = "Orta",
                ProjeId = proje.Id
            };

            context.Ekipler.Add(ekip);
            context.Projeler.Add(proje);
            context.Gorevler.Add(gorev);
            await context.SaveChangesAsync();

            return (ekip.Id, gorev.Id);
        }

        [Fact]
        public async Task HataKaydet_YeniHataAcikDurumdaOlusturulur()
        {
            using var context = YeniContext();
            var (ekipId, gorevId) = await OrnekVeriOlusturAsync(context);
            var servis = new HataService(context);

            var sonuc = await servis.HataKaydetAsync(new HataOlusturDto
            {
                GorevId = gorevId,
                Modul = "Giriş Ekranı",
                Aciklama = "Buton tıklanmıyor",
                Siddet = "Yuksek"
            }, sorumluId: Guid.NewGuid(), callerEkipId: ekipId);

            Assert.True(sonuc.IsSuccess);
            Assert.Equal(HataDurumu.Acik, sonuc.Data!.Durum);
        }

        [Fact]
        public async Task HataKaydet_BaskaEkibinGoreviyseReddedilir()
        {
            using var context = YeniContext();
            var (_, gorevId) = await OrnekVeriOlusturAsync(context);
            var servis = new HataService(context);

            var sonuc = await servis.HataKaydetAsync(new HataOlusturDto
            {
                GorevId = gorevId,
                Modul = "X",
                Aciklama = "Y",
                Siddet = "Orta"
            }, sorumluId: Guid.NewGuid(), callerEkipId: Guid.NewGuid());

            Assert.False(sonuc.IsSuccess);
        }

        [Fact]
        public async Task HataDurumGuncelle_AcikTanCozulduyeGecer()
        {
            using var context = YeniContext();
            var (ekipId, gorevId) = await OrnekVeriOlusturAsync(context);
            var servis = new HataService(context);

            var kayit = await servis.HataKaydetAsync(new HataOlusturDto
            {
                GorevId = gorevId,
                Modul = "X",
                Aciklama = "Y",
                Siddet = "Dusuk"
            }, sorumluId: Guid.NewGuid(), callerEkipId: ekipId);

            var guncelleSonucu = await servis.HataDurumGuncelleAsync(kayit.Data!.Id, HataDurumu.Cozuldu, ekipId);
            Assert.True(guncelleSonucu.IsSuccess);

            var guncelListe = await servis.GetHatalarByGorevIdAsync(gorevId, ekipId);
            Assert.Equal(HataDurumu.Cozuldu, guncelListe.Data![0].Durum);
        }

        [Fact]
        public async Task HataDurumGuncelle_BaskaEkipDenErisimReddedilir()
        {
            using var context = YeniContext();
            var (ekipId, gorevId) = await OrnekVeriOlusturAsync(context);
            var servis = new HataService(context);

            var kayit = await servis.HataKaydetAsync(new HataOlusturDto
            {
                GorevId = gorevId,
                Modul = "X",
                Aciklama = "Y",
                Siddet = "Dusuk"
            }, sorumluId: Guid.NewGuid(), callerEkipId: ekipId);

            var guncelleSonucu = await servis.HataDurumGuncelleAsync(kayit.Data!.Id, HataDurumu.Cozuldu, Guid.NewGuid());
            Assert.False(guncelleSonucu.IsSuccess);
        }
    }
}
