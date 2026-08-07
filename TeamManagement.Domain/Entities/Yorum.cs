using System;

namespace TeamManagement.Domain.Entities
{
    public class Yorum : BaseEntity
    {
        public string Icerik { get; set; }
        public bool Duzenlendi { get; set; }

        //ikincil anahtar
        public Guid GorevId { get; set; }
        public Gorev Gorev { get; set; }

        public Guid YazanId { get; set; }
        public Kullanici YazanKullanici { get; set; }
    }
}
