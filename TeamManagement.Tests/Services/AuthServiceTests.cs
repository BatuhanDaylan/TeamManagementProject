using Microsoft.EntityFrameworkCore;
using TeamManagement.Application.DTOs.Auth;
using TeamManagement.Infrastructure.Persistence;
using TeamManagement.Infrastructure.Services;
using TeamManagement.Tests.Helpers;

namespace TeamManagement.Tests.Services
{
    public class AuthServiceTests
    {
        //Her test kendi izole InMemory veritabanını kullanır, testler birbirini etkilemez
        private static TeamManagementIdentityDbContext YeniContext()
        {
            var options = new DbContextOptionsBuilder<TeamManagementIdentityDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new TeamManagementIdentityDbContext(options);
        }

        private static AuthService YeniServis(TeamManagementIdentityDbContext context)
        {
            var config = new FakeConfiguration(new Dictionary<string, string?>
            {
                //HMACSHA256 için yeterli uzunlukta bir test anahtarı
                ["Jwt:Secret"] = "bu-test-ortami-icin-yeterince-uzun-bir-jwt-gizli-anahtaridir-123456"
            });
            return new AuthService(context, config, new FakeEmailService());
        }

        [Fact]
        public async Task Register_YeniKullaniciOlusturur_SifreyiHashler()
        {
            using var context = YeniContext();
            var servis = YeniServis(context);

            var sonuc = await servis.RegisterAsync(new RegisterDto
            {
                Ad = "Test",
                Soyad = "Kullanici",
                Email = "test@ornek.com",
                Sifre = "sifre123"
            });

            Assert.True(sonuc.IsSuccess);
            var kaydedilen = await context.Kullanicilar.FirstOrDefaultAsync(k => k.Email == "test@ornek.com");
            Assert.NotNull(kaydedilen);
            //Düz metin şifre asla veritabanına yazılmamalı
            Assert.NotEqual("sifre123", kaydedilen!.SifreHash);
            Assert.True(BCrypt.Net.BCrypt.Verify("sifre123", kaydedilen.SifreHash));
        }

        [Fact]
        public async Task Register_AyniEmailIleTekrarKayitOlunamaz()
        {
            using var context = YeniContext();
            var servis = YeniServis(context);

            await servis.RegisterAsync(new RegisterDto
            {
                Ad = "Test",
                Soyad = "Kullanici",
                Email = "test@ornek.com",
                Sifre = "sifre123"
            });

            var ikinciSonuc = await servis.RegisterAsync(new RegisterDto
            {
                Ad = "Baska",
                Soyad = "Kisi",
                Email = "test@ornek.com",
                Sifre = "farkliSifre"
            });

            Assert.False(ikinciSonuc.IsSuccess);
        }

        [Fact]
        public async Task Login_DogruBilgilerleTokenDoner()
        {
            using var context = YeniContext();
            var servis = YeniServis(context);

            await servis.RegisterAsync(new RegisterDto
            {
                Ad = "Test",
                Soyad = "Kullanici",
                Email = "test@ornek.com",
                Sifre = "sifre123"
            });

            var girisSonucu = await servis.LoginAsync(new LoginDto
            {
                Email = "test@ornek.com",
                Sifre = "sifre123"
            });

            Assert.True(girisSonucu.IsSuccess);
            Assert.False(string.IsNullOrEmpty(girisSonucu.Data!.Token));
        }

        [Fact]
        public async Task Login_YanlisSifreyleBasarisizOlur()
        {
            using var context = YeniContext();
            var servis = YeniServis(context);

            await servis.RegisterAsync(new RegisterDto
            {
                Ad = "Test",
                Soyad = "Kullanici",
                Email = "test@ornek.com",
                Sifre = "sifre123"
            });

            var girisSonucu = await servis.LoginAsync(new LoginDto
            {
                Email = "test@ornek.com",
                Sifre = "yanlisSifre"
            });

            Assert.False(girisSonucu.IsSuccess);
        }

        [Fact]
        public async Task SifreDegistir_MevcutSifreYanlisIseReddedilir()
        {
            using var context = YeniContext();
            var servis = YeniServis(context);

            await servis.RegisterAsync(new RegisterDto
            {
                Ad = "Test",
                Soyad = "Kullanici",
                Email = "test@ornek.com",
                Sifre = "sifre123"
            });
            var kullanici = await context.Kullanicilar.FirstAsync(k => k.Email == "test@ornek.com");

            var sonuc = await servis.SifreDegistirAsync(kullanici.Id, new SifreDegistirDto
            {
                MevcutSifre = "yanlisSifre",
                YeniSifre = "yeniSifre123"
            });

            Assert.False(sonuc.IsSuccess);
        }
    }
}
