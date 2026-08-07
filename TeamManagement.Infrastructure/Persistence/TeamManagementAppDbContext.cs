using Microsoft.EntityFrameworkCore;
using TeamManagement.Domain.Entities;

namespace TeamManagement.Infrastructure.Persistence
{
    public class TeamManagementAppDbContext : DbContext
    {
        public TeamManagementAppDbContext(DbContextOptions<TeamManagementAppDbContext> options) : base(options)
        {
        }

        public DbSet<Ekip> Ekipler { get; set; }
        public DbSet<EkipOzetGecmisi> EkipOzetGecmisleri { get; set; }
        public DbSet<Davet> Davetler { get; set; }
        public DbSet<Proje> Projeler { get; set; }
        public DbSet<Gorev> Gorevler { get; set; }
        public DbSet<GorevIlerleme> GorevIlerlemeleri { get; set; }
        public DbSet<Yorum> Yorumlar { get; set; }
        public DbSet<Bildirim> Bildirimler { get; set; }
        public DbSet<Hata> Hatalar { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //Bu veritabanında "Kullanici" tablosu olmadığı için bağlantıları yoksayıyoruz
            modelBuilder.Entity<Gorev>().Ignore(g => g.AtananKullanici);
            modelBuilder.Entity<GorevIlerleme>().Ignore(gi => gi.EkleyenKullanici);
            modelBuilder.Entity<Yorum>().Ignore(y => y.YazanKullanici);
            modelBuilder.Entity<Bildirim>().Ignore(b => b.Kullanici);
            modelBuilder.Entity<Ekip>().Ignore(e => e.Kullanicilar);
        }
    }
}
