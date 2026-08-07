namespace TeamManagement.Application.DTOs.Auth
{
    public class GuncelleProfilDto
    {
        public string Ad { get; set; } = string.Empty;
        public string Soyad { get; set; } = string.Empty;
    }

    public class SifreDegistirDto
    {
        public string MevcutSifre { get; set; } = string.Empty;
        public string YeniSifre { get; set; } = string.Empty;
    }
}
