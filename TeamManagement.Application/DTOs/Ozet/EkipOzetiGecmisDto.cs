namespace TeamManagement.Application.DTOs.Ozet
{
    public class EkipOzetiGecmisDto
    {
        public Guid Id { get; set; }
        public string Icerik { get; set; } = string.Empty;
        public DateTime OlusturulmaTarihi { get; set; }
    }
}
