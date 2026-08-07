using System;
using System.Collections.Generic;

namespace TeamManagement.Domain.Entities
{
    public class Proje : BaseEntity
    {
        public string Ad { get; set; }
        public string? Aciklama { get; set; }

        //ikincil anahtar
        public Guid EkipId { get; set; }
        public Ekip Ekip { get; set; }

        public ICollection<Gorev> Gorevler { get; set; }
    }
}
