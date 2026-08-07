using Microsoft.EntityFrameworkCore;
using TeamManagement.Application.DTOs.Bildirim;
using TeamManagement.Application.Interfaces;
using TeamManagement.Domain.Entities;
using TeamManagement.Infrastructure.Persistence;
using TeamManagement.Application.Common.Results;

namespace TeamManagement.Infrastructure.Services
{
    //Bildirim servisi oluşturma listeleme okundu işaretleme ve sayaç işlemlerini yönetir
    public class BildirimService : IBildirimService
    {
        //Bildirim verilerinin tutulduğu uygulama veritabanı bağlamı
        private readonly TeamManagementAppDbContext _appContext;

        public BildirimService(TeamManagementAppDbContext appContext)
        {
            _appContext = appContext;
        }

        //Belirtilen kullanıcı için yeni bildirim oluşturur ve kaydeder
        public async Task<Result> BildirimOlusturAsync(Guid kullaniciId, string icerik, Guid? gorevId = null)
        {
            var bildirim = new Bildirim
            {
                KullaniciId = kullaniciId,
                Icerik = icerik,
                GorevId = gorevId,
                //Yeni oluşturulan bildirim varsayılan olarak okunmamış gelir
                Okundu = false,
                OluşturulmaTarihi = DateTime.UtcNow
            };

            _appContext.Bildirimler.Add(bildirim);
            await _appContext.SaveChangesAsync();
            return Result.Success();
        }

        //Kullanıcının tüm bildirimlerini en yeni en üstte olacak şekilde listeler
        public async Task<Result<List<BildirimDto>>> GetBildirimlerAsync(Guid kullaniciId)
        {
            var bildirimler = await _appContext.Bildirimler
                .Where(b => b.KullaniciId == kullaniciId)
                .OrderByDescending(b => b.OluşturulmaTarihi)
                .Select(b => new BildirimDto
                {
                    Id = b.Id,
                    Icerik = b.Icerik,
                    Okundu = b.Okundu,
                    GorevId = b.GorevId,
                    OlusturulmaTarihi = b.OluşturulmaTarihi
                })
                .ToListAsync();

            return Result<List<BildirimDto>>.Success(bildirimler);
        }

        //Belirtilen bildirimi okundu olarak işaretler sahiplik kontrolü ile başkasının bildirimi işaretlenemez
        public async Task<Result> OkunduIsaretle(Guid bildirimId, Guid kullaniciId)
        {
            //Hem bildirim ID hem de kullanıcı ID eşleşmeli güvenlik kontrolü
            var bildirim = await _appContext.Bildirimler
                .FirstOrDefaultAsync(b => b.Id == bildirimId && b.KullaniciId == kullaniciId);

            if (bildirim == null) return Result.Failure("Bildirim bulunamadı veya yetkiniz yok.", 404);

            bildirim.Okundu = true;
            await _appContext.SaveChangesAsync();
            return Result.Success();
        }

        //Kullanıcının tüm okunmamış bildirimlerini tek sorguda okundu işaretler
        public async Task<Result> TumunuOkunduIsaretleAsync(Guid kullaniciId)
        {
            await _appContext.Bildirimler
                .Where(b => b.KullaniciId == kullaniciId && !b.Okundu)
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.Okundu, true));

            return Result.Success();
        }

        //Kullanıcının okunmamış bildirim sayısını döndürür navbar rozeti için kullanılır
        public async Task<Result<int>> GetOkunmamisSayisiAsync(Guid kullaniciId)
        {
            var sayi = await _appContext.Bildirimler
                .CountAsync(b => b.KullaniciId == kullaniciId && !b.Okundu);

            return Result<int>.Success(sayi);
        }
    }
}
