using System;
using System.Collections.Generic;

namespace TeamManagement.Domain.Entities
{
    public class Ekip : BaseEntity
    {
        public string Ad { get; set; }
        public Guid OlusturanLiderId { get; set; }
        //Haftalık otomatik özet mailinin en son gönderildiği tarih; aynı hafta içinde tekrar gönderilmesini önler
        public DateTime? SonOzetGonderimTarihi { get; set; }

        //navigasyon
        public ICollection<Kullanici> Kullanicilar { get; set; }
        public ICollection<Proje> Projeler { get; set; }
        public ICollection<Davet> Davetler { get; set; }
    }
}
