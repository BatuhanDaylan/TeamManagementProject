namespace TeamManagement.Application.DTOs.Ayarlar
{
    //Şifreler asla client'a geri dönmez, sadece yapılandırılmış olup olmadığı bilgisi döner
    public class MailAyarlariDurumDto
    {
        public bool MailYapilandirildiMi { get; set; }
        public bool AiApiKeyYapilandirildiMi { get; set; }
        public string KayitliEmail { get; set; } = string.Empty;
    }
}
