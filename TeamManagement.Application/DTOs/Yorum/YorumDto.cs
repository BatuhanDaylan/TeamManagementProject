namespace TeamManagement.Application.DTOs.Yorum
{
    public class YorumOlusturDto
    {
        public Guid GorevId { get; set; }
        public string Icerik { get; set; }
    }

    public class YorumDto
    {
        public Guid Id { get; set; }
        public Guid GorevId { get; set; }
        public Guid YazanId { get; set; }
        public string YazanAdSoyad { get; set; } = string.Empty;
        public string Icerik { get; set; }
        public DateTime OlusturulmaTarihi { get; set; }
        //Yorum sonradan düzenlendiyse "(düzenlendi)" göstermek için kullanılır
        public bool Duzenlendi { get; set; }
    }

    public class UpdateYorumDto
    {
        public string Icerik { get; set; } = string.Empty;
    }
}
