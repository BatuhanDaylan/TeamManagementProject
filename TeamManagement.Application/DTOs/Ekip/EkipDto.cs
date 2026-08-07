namespace TeamManagement.Application.DTOs.Ekipler
{
    public class EkipDto
    {
        public Guid Id { get; set; }
        public string Ad { get; set; }
        public Guid OlusturanLiderId { get; set; }
        public DateTime OluşturulmaTarihi { get; set; }
    }
}
