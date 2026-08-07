using System;
using TeamManagement.Domain.Enums;

namespace TeamManagement.Application.DTOs.Auth
{
    public class KullaniciDto
    {
        public Guid Id { get; set; }
        public string Ad { get; set; }
        public string Soyad { get; set; }
        public string Email { get; set; }
        public string Rol { get; set; }
        public Guid? EkipId { get; set; }
    }
}
