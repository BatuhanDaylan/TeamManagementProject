namespace TeamManagement.Application.DTOs.Projeler
{
    public class CreateProjeDto
    {
        public string Ad { get; set; }
        public string Aciklama { get; set; }
        // Projenin hangi ekibe bağlanacağını belirtiyoruz
        public Guid EkipId { get; set; }
    }

    public class UpdateProjeDto
    {
        public string Ad { get; set; } = string.Empty;
        public string? Aciklama { get; set; }
    }
}
