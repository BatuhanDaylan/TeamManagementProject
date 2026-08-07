namespace TeamManagement.Application.DTOs.Bildirim
{
    public class BildirimDto
    {
        public Guid Id { get; set; }
        public string Icerik { get; set; }
        public bool Okundu { get; set; }
        public Guid? GorevId { get; set; }
        public DateTime OlusturulmaTarihi { get; set; }
    }
}
