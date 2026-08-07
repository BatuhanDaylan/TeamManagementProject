namespace TeamManagement.Application.DTOs.Gorevler
{
    public class UpdateGorevDto
    {
        public string? Baslik { get; set; }
        public string? Aciklama { get; set; }
        public string? Oncelik { get; set; }
        //Null gonderilirse guncellenmez mevcut deger korunur
        public DateTime? BaslangicTarihi { get; set; }
        public DateTime? Deadline { get; set; }
        public Guid? AtananId { get; set; }
    }
}
