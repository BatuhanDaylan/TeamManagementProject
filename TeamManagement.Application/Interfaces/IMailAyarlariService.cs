using TeamManagement.Application.DTOs.Ayarlar;
using TeamManagement.Application.Common.Results;

namespace TeamManagement.Application.Interfaces
{
    //Lider'in haftalık plan mailini kendi Gönderilenler klasöründen okutabilmesi için gereken
    //IMAP uygulama şifresi ve kişisel AI API anahtarı ayarlarını yönetir
    public interface IMailAyarlariService
    {
        Task<Result<MailAyarlariDurumDto>> GetDurumAsync(Guid kullaniciId);
        Task<Result> KaydetAsync(Guid kullaniciId, MailAyarlariDto dto);
        Task<Result> KaldirAsync(Guid kullaniciId);
    }
}
