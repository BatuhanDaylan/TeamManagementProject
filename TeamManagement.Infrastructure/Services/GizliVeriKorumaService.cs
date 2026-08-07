using Microsoft.AspNetCore.DataProtection;
using TeamManagement.Application.Interfaces;

namespace TeamManagement.Infrastructure.Services
{
    //ASP.NET Core Data Protection API'sini sarmalar, tüm şifreleme/çözme tek purpose-string ile tek yerden yapılır
    public class GizliVeriKorumaService : IGizliVeriKorumaService
    {
        private readonly IDataProtector _protector;

        public GizliVeriKorumaService(IDataProtectionProvider provider)
        {
            _protector = provider.CreateProtector("KullaniciMailAyarlari.v1");
        }

        public string Sifrele(string duzMetin)
        {
            return _protector.Protect(duzMetin);
        }

        public string? SifreCoz(string? sifreliMetin)
        {
            if (string.IsNullOrEmpty(sifreliMetin)) return null;
            return _protector.Unprotect(sifreliMetin);
        }
    }
}
