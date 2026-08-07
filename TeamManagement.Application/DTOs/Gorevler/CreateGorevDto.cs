namespace TeamManagement.Application.DTOs.Gorevler
{
    public class CreateGorevDto
    {
        public string Baslik { get; set; }
        public string Aciklama { get; set; }
        public string Oncelik { get; set; }
        //Baslangic ve bitis tarihleri opsiyoneldir timeline icin kullanilir
        public DateTime? BaslangicTarihi { get; set; }
        public DateTime? Deadline { get; set; }
        public Guid ProjeId { get; set; }
        public Guid? AtananId { get; set; }
    }
}
