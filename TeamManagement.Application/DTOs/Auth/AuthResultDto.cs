namespace TeamManagement.Application.DTOs.Auth
{
    public class AuthResultDto
    {
        public bool BasariliMi { get; set; }
        public string Mesaj { get; set; }
        public string Token { get; set; }
        public KullaniciDto Kullanici { get; set; }
    }
}
