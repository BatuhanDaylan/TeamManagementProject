using System;

namespace TeamManagement.Domain.Entities
{
    public class GorevIlerleme : BaseEntity
    {
        public string TamamlananKisim { get; set; } //AI'ın mailden çıkarttığı madde
        public string KaynakEmailMetni { get; set; } //Referans için orijinal mailin kendisi

        //Hangi göreve ait?
        public Guid GorevId { get; set; }
        public Gorev Gorev { get; set; }

        //Maili atan kişiyi eşleştirebilirsek
        public Guid? EkleyenKullaniciId { get; set; }
        public Kullanici EkleyenKullanici { get; set; }
    }
}
