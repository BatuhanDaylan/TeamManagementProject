using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TeamManagement.Application.DTOs.Auth;
using TeamManagement.Application.Interfaces;
using TeamManagement.Domain.Entities;
using TeamManagement.Domain.Enums;
using TeamManagement.Infrastructure.Persistence;
using TeamManagement.Application.Common.Results;

namespace TeamManagement.Infrastructure.Services
{
    //Kimlik doğrulama servisi kayıt giriş ve JWT token üretimi işlemlerini yönetir
    public class AuthService : IAuthService
    {
        //Kullanıcı kimlik verilerini içeren veritabanı bağlamı
        private readonly TeamManagementIdentityDbContext _context;

        //JWT secret ve diğer konfigürasyon değerlerini appsettings ten okumak için
        private readonly IConfiguration _configuration;

        //Şifre sıfırlama bağlantısını e-posta ile göndermek için
        private readonly IEmailService _emailService;

        public AuthService(TeamManagementIdentityDbContext context, IConfiguration configuration, IEmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
        }

        //Yeni kullanıcı kaydı oluşturur e-posta tekrar kontrolü ve BCrypt şifreleme yapar
        public async Task<Result<AuthResultDto>> RegisterAsync(RegisterDto registerDto)
        {
            //Aynı e-posta adresiyle kayıt varsa hata döndür
            var userExists = await _context.Kullanicilar.AnyAsync(k => k.Email == registerDto.Email);
            if (userExists)
            {
                return Result<AuthResultDto>.Failure("Bu email adresi zaten kullanılıyor.");
            }

            //Şifreyi BCrypt ile hashle asla düz metin saklanmaz
            var yeniKullanici = new Kullanici
            {
                Id = Guid.NewGuid(),
                Ad = registerDto.Ad,
                Soyad = registerDto.Soyad,
                Email = registerDto.Email,
                SifreHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Sifre),
                Rol = Rol.Uye,
                OluşturulmaTarihi = DateTime.UtcNow
            };

            _context.Kullanicilar.Add(yeniKullanici);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                //Yukarıdaki AnyAsync kontrolü ile buradaki kayıt arasında eşzamanlı bir istek aynı
                //e-postayla kayıt oluşturduysa veritabanındaki benzersizlik kısıtı burada devreye girer
                return Result<AuthResultDto>.Failure("Bu email adresi zaten kullanılıyor.", 400);
            }

            var resultDto = new AuthResultDto { BasariliMi = true, Mesaj = "Kayıt başarılı!" };
            return Result<AuthResultDto>.Success(resultDto);
        }

        //Giriş işlemi e-posta ile kullanıcı bulur şifreyi doğrular başarılıysa JWT döndürür
        public async Task<Result<AuthResultDto>> LoginAsync(LoginDto loginDto)
        {
            //Kullanıcıyı e-posta adresine göre bul
            var kullanici = await _context.Kullanicilar.FirstOrDefaultAsync(k => k.Email == loginDto.Email);
            if (kullanici == null)
            {
                return Result<AuthResultDto>.Failure("Kullanıcı bulunamadı.", 404);
            }

            //BCrypt ile saklanan hash ile girilen şifreyi karşılaştır
            if (!BCrypt.Net.BCrypt.Verify(loginDto.Sifre, kullanici.SifreHash))
            {
                return Result<AuthResultDto>.Failure("Şifre hatalı.", 401);
            }

            //Doğrulama başarılıysa JWT token üretip döndür
            var token = GenerateJwtToken(kullanici);

            var kullaniciDto = new KullaniciDto
            {
                Id = kullanici.Id,
                Ad = kullanici.Ad,
                Soyad = kullanici.Soyad,
                Email = kullanici.Email,
                Rol = kullanici.Rol.ToString(),
                EkipId = kullanici.EkipId
            };

            var resultDto = new AuthResultDto
            {
                BasariliMi = true,
                Mesaj = "Giriş başarılı.",
                Token = token,
                Kullanici = kullaniciDto
            };
            return Result<AuthResultDto>.Success(resultDto);
        }

        //Şifre sıfırlama talebi: kullanıcı bulunamasa bile başarılı döner ki e-posta adresinin
        //sistemde kayıtlı olup olmadığı dışarıdan anlaşılamasın (enumeration saldırısını önler)
        public async Task<Result> SifreSifirlamaTalepEtAsync(string email)
        {
            var kullanici = await _context.Kullanicilar.FirstOrDefaultAsync(k => k.Email == email);
            if (kullanici == null) return Result.Success();

            //Davet token'ıyla aynı desende URL güvenli rastgele token üretilir
            var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("+", "-").Replace("/", "_").Replace("=", "");

            kullanici.SifreSifirlamaToken = token;
            //Token 1 saat geçerlidir, davet token'ından kısa çünkü hesaba erişim riski taşır
            kullanici.SifreSifirlamaSonKullanma = DateTime.UtcNow.AddHours(1);
            await _context.SaveChangesAsync();

            var sifirlamaLinki = $"http://localhost:5173/sifre-sifirla?token={token}";
            var htmlBody = $"""
                <h2>Şifre Sıfırlama Talebi</h2>
                <p>Hesabınız için bir şifre sıfırlama talebinde bulunuldu. Aşağıdaki bağlantıya tıklayarak
                   yeni bir şifre belirleyebilirsiniz. Bu bağlantı <strong>1 saat</strong> geçerlidir.</p>
                <a href="{sifirlamaLinki}" style="
                    display: inline-block;
                    padding: 12px 24px;
                    background-color: #6366f1;
                    color: white;
                    text-decoration: none;
                    border-radius: 6px;">Şifremi Sıfırla</a>
                <p>Bu talebi siz oluşturmadıysanız bu e-postayı görmezden gelebilirsiniz.</p>
                """;

            await _emailService.SendAsync(kullanici.Email, "Şifre Sıfırlama Talebi", htmlBody);
            return Result.Success();
        }

        //Token geçerliyse şifreyi günceller ve token'ı tüketir (tek kullanımlık)
        public async Task<Result> SifreSifirlaAsync(SifreSifirlaDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.YeniSifre) || dto.YeniSifre.Length < 6)
                return Result.Failure("Şifre en az 6 karakter olmalıdır.", 400);

            var kullanici = await _context.Kullanicilar
                .FirstOrDefaultAsync(k => k.SifreSifirlamaToken == dto.Token);

            if (kullanici == null || kullanici.SifreSifirlamaSonKullanma == null || kullanici.SifreSifirlamaSonKullanma < DateTime.UtcNow)
                return Result.Failure("Bağlantının süresi dolmuş veya geçersiz. Lütfen yeni bir talep oluşturun.", 400);

            kullanici.SifreHash = BCrypt.Net.BCrypt.HashPassword(dto.YeniSifre);
            kullanici.SifreSifirlamaToken = null;
            kullanici.SifreSifirlamaSonKullanma = null;
            await _context.SaveChangesAsync();

            return Result.Success();
        }

        //Kullanıcının kendi ad/soyad bilgisini günceller (e-posta ve rol burada değiştirilemez)
        public async Task<Result<KullaniciDto>> ProfilGuncelleAsync(Guid kullaniciId, GuncelleProfilDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Ad) || string.IsNullOrWhiteSpace(dto.Soyad))
                return Result<KullaniciDto>.Failure("Ad ve soyad boş olamaz.", 400);

            var kullanici = await _context.Kullanicilar.FindAsync(kullaniciId);
            if (kullanici == null) return Result<KullaniciDto>.Failure("Kullanıcı bulunamadı.", 404);

            kullanici.Ad = dto.Ad.Trim();
            kullanici.Soyad = dto.Soyad.Trim();
            await _context.SaveChangesAsync();

            return Result<KullaniciDto>.Success(new KullaniciDto
            {
                Id = kullanici.Id,
                Ad = kullanici.Ad,
                Soyad = kullanici.Soyad,
                Email = kullanici.Email,
                Rol = kullanici.Rol.ToString(),
                EkipId = kullanici.EkipId
            });
        }

        //Mevcut şifre doğrulanmadan değişikliğe izin verilmez
        public async Task<Result> SifreDegistirAsync(Guid kullaniciId, SifreDegistirDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.YeniSifre) || dto.YeniSifre.Length < 6)
                return Result.Failure("Yeni şifre en az 6 karakter olmalıdır.", 400);

            var kullanici = await _context.Kullanicilar.FindAsync(kullaniciId);
            if (kullanici == null) return Result.Failure("Kullanıcı bulunamadı.", 404);

            if (!BCrypt.Net.BCrypt.Verify(dto.MevcutSifre, kullanici.SifreHash))
                return Result.Failure("Mevcut şifre hatalı.", 401);

            kullanici.SifreHash = BCrypt.Net.BCrypt.HashPassword(dto.YeniSifre);
            await _context.SaveChangesAsync();

            return Result.Success();
        }

        //JWT token üretimi kullanıcı kimliği e-posta ve rol bilgisini claim olarak ekler
        private string GenerateJwtToken(Kullanici kullanici)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            //Secret appsettings ten okunur hardcoded yazılmaz
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"]!);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                //Token içine kullanıcı kimliği e-posta ve rol bilgisi gömülür
                //EkipId claim'i, kaynak sahiplik kontrollerinde (yetkisiz erişimi engellemek için)
                //her istekte veritabanına gitmeden token üzerinden okunabilsin diye eklenir
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, kullanici.Id.ToString()),
                    new Claim(ClaimTypes.Email, kullanici.Email),
                    new Claim(ClaimTypes.Role, kullanici.Rol.ToString()),
                    new Claim("EkipId", kullanici.EkipId?.ToString() ?? "")
                }),
                //Token 7 gün geçerli olur
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}