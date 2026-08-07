using TeamManagement.Application.Common.Results;
using TeamManagement.Application.Interfaces;

namespace TeamManagement.Tests.Helpers
{
    //Gerçek SMTP bağlantısı kurmadan test edebilmek için IEmailService'in sahte implementasyonu
    public class FakeEmailService : IEmailService
    {
        public List<(string To, string Subject)> GonderilenMailler { get; } = new();

        public Task<Result> SendAsync(string to, string subject, string htmlBody,
            string? senderEmail = null, string? senderPassword = null, string? senderName = null)
        {
            GonderilenMailler.Add((to, subject));
            return Task.FromResult(Result.Success());
        }
    }
}
