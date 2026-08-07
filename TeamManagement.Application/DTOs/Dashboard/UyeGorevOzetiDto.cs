namespace TeamManagement.Application.DTOs.Dashboard
{
    public class UyeGorevOzetiDto
    {
        public Guid KullaniciId { get; set; }
        public string Ad { get; set; } = string.Empty;
        public string Soyad { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public List<TeamManagement.Application.DTOs.Gorevler.GorevDto> AktifGorevler { get; set; } = new();
    }
}
