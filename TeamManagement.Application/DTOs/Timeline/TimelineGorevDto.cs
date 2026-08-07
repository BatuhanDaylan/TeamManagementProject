namespace TeamManagement.Application.DTOs.Timeline
{
    public class TimelineGorevDto
    {
        public Guid Id { get; set; }
        public string Baslik { get; set; } = string.Empty;
        public Guid ProjeId { get; set; }
        public string ProjeAdi { get; set; } = string.Empty;
        public Guid? AtananId { get; set; }
        public string AtananAdSoyad { get; set; } = string.Empty;
        public DateTime? BaslangicTarihi { get; set; }
        public DateTime? Deadline { get; set; }
        public DateTime OlusturulmaTarihi { get; set; }
        public TeamManagement.Domain.Enums.GorevDurumu Durum { get; set; }
        public string Oncelik { get; set; } = string.Empty;
    }
}
