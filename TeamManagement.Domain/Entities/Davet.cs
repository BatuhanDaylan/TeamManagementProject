using System;

namespace TeamManagement.Domain.Entities
{
    public class Davet : BaseEntity
    {
        public string Email { get; set; }
        public string Token { get; set; }
        public DateTime SonKullanmaTarihi { get; set; }
        public bool Kullanildi { get; set; } = false;

        public Guid EkipId { get; set; }
        public Ekip Ekip { get; set; }
    }
}