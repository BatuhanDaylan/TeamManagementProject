namespace TeamManagement.Application.DTOs.Davet
{
    public class DavetGonderDto
    {
        public string Email { get; set; }
        public Guid EkipId { get; set; }
    }

    public class DavetDogrulamaDto
    {
        public bool GecerliMi { get; set; }
        public string Email { get; set; }
        public Guid EkipId { get; set; }
        public string Mesaj { get; set; }
    }

    public class DavetKabulDto
    {
        public string Token { get; set; }
        public string Ad { get; set; }
        public string Soyad { get; set; }
        public string YeniSifre { get; set; }
    }

    public class DavetListeDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public DateTime SonKullanmaTarihi { get; set; }
        public bool Kullanildi { get; set; }
        public Guid EkipId { get; set; }
    }
}
