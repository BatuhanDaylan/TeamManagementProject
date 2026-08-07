using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using TeamManagement.Application.Interfaces;
using TeamManagement.Application.Common.Results;

namespace TeamManagement.Infrastructure.Services
{
    //SMTP üzerinden HTML e-posta gönderen servis konfigürasyon appsettings ten okunur
    public class SmtpEmailService : IEmailService
    {
        //SMTP host port ve kimlik bilgilerini okumak için konfigürasyon
        private readonly IConfiguration _configuration;

        public SmtpEmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        //Belirtilen alıcıya konu ve HTML gövdesiyle e-posta gönderir
        //senderEmail/senderPassword verilirse o hesap üzerinden (örn. liderin kendi uygulama şifresi),
        //verilmezse appsettings'teki ortak bot hesabı üzerinden gönderilir
        public async Task<Result> SendAsync(string to, string subject, string htmlBody,
            string? senderEmail = null, string? senderPassword = null, string? senderName = null)
        {
            try
            {
                //SMTP host/port her zaman ortaktır, sadece kimlik bilgileri hesaba göre değişebilir
                var smtpHost = _configuration["EmailSettings:SmtpHost"]!;
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]!);
                senderEmail ??= _configuration["EmailSettings:SenderEmail"]!;
                senderPassword ??= _configuration["EmailSettings:SenderPassword"]!;
                senderName ??= _configuration["EmailSettings:SenderName"] ?? "Prizma Pano";

                //MimeKit ile e-posta mesajı oluşturulur
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, senderEmail));
                message.To.Add(new MailboxAddress(to, to));
                message.Subject = subject;

                //HTML gövdesi BodyBuilder ile hazırlanır
                var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(senderEmail, senderPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure($"Email gönderilirken hata oluştu: {ex.Message}");
            }
        }
    }
}
