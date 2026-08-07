using Microsoft.EntityFrameworkCore;
using TeamManagement.Domain.Entities;

namespace TeamManagement.Infrastructure.Persistence
{
    public class TeamManagementIdentityDbContext : DbContext
    {
        public TeamManagementIdentityDbContext(DbContextOptions<TeamManagementIdentityDbContext> options) : base(options)
        {
        }

        public DbSet<Kullanici> Kullanicilar { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //Domain nesnesindeki diğer veritabanına ait bağlantıları yoksayıyoruz
            modelBuilder.Entity<Kullanici>().Ignore(k => k.Ekip);

            //Aynı e-posta adresiyle iki kayıt oluşmasını uygulama kodundaki (racy) kontrole güvenmeden
            //veritabanı seviyesinde de engeller (davet kabul, admin kayıt, register akışları eşzamanlı
            //çalışırsa uygulama-seviyesi "zaten var mı" kontrolü tek başına yeterli değildir)
            modelBuilder.Entity<Kullanici>().HasIndex(k => k.Email).IsUnique();
        }
    }
}
