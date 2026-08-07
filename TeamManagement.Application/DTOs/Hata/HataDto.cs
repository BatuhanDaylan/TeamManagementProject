using TeamManagement.Domain.Enums;

namespace TeamManagement.Application.DTOs.Hata
{
    public class HataOlusturDto
    {
        public Guid GorevId { get; set; }
        public string Modul { get; set; }
        public string Aciklama { get; set; }
        public string Siddet { get; set; }
    }

    public class UpdateHataDurumDto
    {
        public HataDurumu YeniDurum { get; set; }
    }

    public class HataDto
    {
        public Guid Id { get; set; }
        public Guid GorevId { get; set; }
        public Guid SorumluUyeId { get; set; }
        public string Modul { get; set; }
        public string Aciklama { get; set; }
        public string Siddet { get; set; }
        public HataDurumu Durum { get; set; }
        public DateTime OlusturulmaTarihi { get; set; }
    }

    public class HataIstatistikDto
    {
        public string Modul { get; set; }
        public int HataSayisi { get; set; }
    }

    public class KisiHataIstatistikDto
    {
        public Guid KullaniciId { get; set; }
        public int HataSayisi { get; set; }
    }
}
