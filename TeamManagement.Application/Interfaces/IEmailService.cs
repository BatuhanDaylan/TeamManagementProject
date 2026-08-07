using TeamManagement.Application.Common.Results;

namespace TeamManagement.Application.Interfaces
{
    public interface IEmailService
    {
        //senderEmail/senderPassword verilirse (örn. liderin kendi Ayarlar'da kayıtlı uygulama şifresi)
        //o hesap üzerinden gönderilir; verilmezse appsettings'teki ortak bot hesabına düşülür
        Task<Result> SendAsync(string to, string subject, string htmlBody,
            string? senderEmail = null, string? senderPassword = null, string? senderName = null);
    }
}
