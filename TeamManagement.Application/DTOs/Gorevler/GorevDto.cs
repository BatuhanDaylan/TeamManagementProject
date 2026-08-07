using TeamManagement.Domain.Enums;

namespace TeamManagement.Application.DTOs.Gorevler
{
    public class GorevDto
    {
        public Guid Id { get; set; }
        public string Baslik { get; set; }
        public string Aciklama { get; set; }
        public GorevDurumu Durum { get; set; }
        public string Oncelik { get; set; }
        //Gorev baslangic ve bitis tarihleri timeline ve dashboard icin kullanilir
        public DateTime? BaslangicTarihi { get; set; }
        public DateTime? Deadline { get; set; }
        public Guid ProjeId { get; set; }
        //Sadece Dashboard uç noktasında doldurulur, diğer uç noktalarda null gelir
        public string? ProjeAdi { get; set; }
        public Guid? AtananId { get; set; }
        public DateTime OluşturulmaTarihi { get; set; }
    }
}
