using System;

namespace TeamManagement.Domain.Entities
{
    public class Bildirim : BaseEntity
    {
        public string Icerik { get; set; }
        public bool Okundu { get; set; } = false;
        //Bildirim bir göreve ilişkinse, tıklandığında o göreve gidebilmek için kullanılır
        public Guid? GorevId { get; set; }

        //ikincil anahtar
        public Guid KullaniciId { get; set; }
        public Kullanici Kullanici { get; set; }
    }
}
