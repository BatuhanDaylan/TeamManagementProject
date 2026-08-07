using System.Security.Claims;

namespace TeamManagement.Api.Extensions
{
    //Token claim'lerini okumak icin ortak yardimci metotlar, controller'lardaki tekrar eden parse kodunu azaltir
    public static class ClaimsPrincipalExtensions
    {
        public static Guid? GetKullaniciId(this ClaimsPrincipal user)
        {
            var value = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }

        //Kullanicinin tokena gomulu ekip kimligi, kaynak sahiplik kontrollerinde kullanilir
        public static Guid? GetEkipId(this ClaimsPrincipal user)
        {
            var value = user.FindFirst("EkipId")?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }

        public static string GetRol(this ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Role)?.Value ?? "Uye";
        }
    }
}
