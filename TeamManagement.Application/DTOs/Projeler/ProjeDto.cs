namespace TeamManagement.Application.DTOs.Projeler
{
    public class ProjeDto
    {
        public Guid Id { get; set; }
        public string Ad { get; set; }
        public string Aciklama { get; set; }
        public Guid EkipId { get; set; }
        public Guid OlusturanLiderId { get; set; }
        public DateTime OlusturulmaTarihi { get; set; }
    }
}
