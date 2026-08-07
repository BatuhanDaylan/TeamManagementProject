namespace TeamManagement.Application.DTOs.Ayarlar
{
    //null = ilgili alanı değiştirme, "" (boş string) = ilgili alanı kaldır
    public class MailAyarlariDto
    {
        public string? UygulamaSifresi { get; set; }
        public string? AiApiKey { get; set; }
    }
}
