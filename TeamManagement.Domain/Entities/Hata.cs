using System;
using TeamManagement.Domain.Enums;

namespace TeamManagement.Domain.Entities
{
    public class Hata : BaseEntity
    {
        public string Modul { get; set; }
        public string Aciklama { get; set; }
        public string Siddet { get; set; }
        public HataDurumu Durum { get; set; } = HataDurumu.Acik;
        public Guid GorevId { get; set; }
        public Guid Gorev { get; set; }
        public Guid SorumluUyeId { get; set; }
    }
}