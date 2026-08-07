namespace TeamManagement.Application.DTOs.GorevIlerleme
{
    public class GorevIlerlemeDto
    {
        public Guid Id { get; set; }
        public string TamamlananKisim { get; set; } = string.Empty;
        public string? KaynakEmailMetni { get; set; }
        public Guid GorevId { get; set; }
        public Guid? EkleyenKullaniciId { get; set; }
        //Oluşturulma tarihi timeline'da sıralamak için kullanılır
        public DateTime OlusturulmaTarihi { get; set; }
    }
}
