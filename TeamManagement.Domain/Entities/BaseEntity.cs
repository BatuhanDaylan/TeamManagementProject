using System;

namespace TeamManagement.Domain.Entities
{
    public abstract class BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime OluşturulmaTarihi { get; set; } = DateTime.UtcNow;
    }
}